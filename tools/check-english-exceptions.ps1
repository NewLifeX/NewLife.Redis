# 检查运行时消息（异常、日志、控制台、界面提示、埋点标签）是否全部为英文
# 规则：调用点字符串中不得出现 CJK 字符；注释与特性参数（Description/DisplayName/Justification）不受限制。
# 项目文件的 <AssemblyTitle>/<Description> 同样不得含中文（PackageDescription 允许中英混排，不检查）。
# 说明：英文原文是词表键，译文放在各组件 Locales/*.yml；日志经 Logger.Format 自动翻译，异常用 L.T(...) 显式翻译。
#
# 用法：
#   pwsh -File tools/check-english-exceptions.ps1                       # 本仓默认目录，失败退出码 1
#   pwsh -File tools/check-english-exceptions.ps1 -Dir src -List        # 盘点中文消息，不失败
#   pwsh -File tools/check-english-exceptions.ps1 -Dir src -Csv hits.csv # 导出待翻译清单
#   pwsh -File tools/check-english-exceptions.ps1 -Dir src -LocaleDir src/Locales # 同时校验运行时消息均已登记词表
param(
    [String]$Root = (Split-Path -Parent $PSScriptRoot),
    [String[]]$Dir,
    [String[]]$Exclude = @(),
    [String]$MessageCall = 'throw new|new\s+[A-Za-z0-9_.]*Exception\(|: base\(|Console\.Write|MessageBox\.Show|AppendTag\(|NewError\(|NewSpan\(|Debug\.Fail\(|WriteLog\(|OnWrite\(|OnDisconnected\(|XTrace\.|Log\??\.(Write|Error|Info|Debug|Warn|Fatal)|WriteInfoEvent\(|WriteErrorEvent\(|BadRequest\(|WriteResultAsync\(|AppendFormat\(|L\.T\(|ex\s*\?\?=\s*new|ex\s*=\s*new',
    [String[]]$LocaleDir = @(),
    [String[]]$AllowMissing = @("IO_StreamTooLong", "Not enough data to read.", "unknown command", "ERR unknown", "ERR no such key"),
    [Switch]$List,
    [String]$Csv
)

$ErrorActionPreference = "Stop"

function Unescape-Yaml([string]$text) {
    # 与源生成器保持一致：还原 \\ \" \n \r \t \uXXXX
    return [regex]::Replace($text, '\\(u[0-9a-fA-F]{4}|.)', {
        param($m)
        $v = $m.Groups[1].Value
        if ($v.Length -eq 5) { return [string][char][Convert]::ToInt32($v.Substring(1), 16) }
        switch -CaseSensitive ($v) {
            'n' { return "`n" }
            'r' { return "`r" }
            't' { return "`t" }
            '\\' { return '\' }
            '"' { return '"' }
            default { return $m.Value }
        }
    })
}

# 运行时消息必须是词表键，否则该消息在任何语言下都只能回退英文
$localeKeys = $null
if ($LocaleDir -and $LocaleDir.Count -gt 0) {
    $localeKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($item in $LocaleDir) {
        $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $Root $item }
        if (!(Test-Path -LiteralPath $path)) { throw "未找到语言目录：$path" }
        foreach ($file in Get-ChildItem $path -Filter *.yml) {
            foreach ($line in Get-Content $file.FullName -Encoding UTF8) {
                if ($line -match '^\s*"((?:[^"\\]|\\.)*)"\s*:') {
                    [void]$localeKeys.Add((Unescape-Yaml $Matches[1]))
                }
            }
        }
    }
}

$keyPatterns = @(
    'L\.T\(\s*"((?:[^"\\]|\\.)*)"',
    'throw\s+new\s+[A-Za-z0-9_.]*Exception\(\s*"((?:[^"\\]|\\.)*)"',
    'new\s+XException\(\s*"((?:[^"\\]|\\.)*)"',
    '\bWriteLog\(\s*"((?:[^"\\]|\\.)*)"',
    '(?:InfoPrefixed|ErrorPrefixed|WritePrefixed)\([^,]+,\s*"((?:[^"\\]|\\.)*)"',
    'XTrace\.(?:WriteLine|WriteError|WriteDebug|WriteLog)\(\s*"((?:[^"\\]|\\.)*)"',
    '\b(?:Log|log|errLog)\??\.(?:Write|Info|Error|Debug|Warn|Fatal)\(\s*"((?:[^"\\]|\\.)*)"',
    'OnDisconnected\(\s*"((?:[^"\\]|\\.)*)"'
)

if (!$Dir -or $Dir.Count -eq 0) { $Dir = @("NewLife.Redis", "NewLife.Redis.Extensions", "NewLife.Extensions.Caching.Redis") }
$dirs = @()
foreach ($item in $Dir) {
    $path = if ([IO.Path]::IsPathRooted($item)) { $item } else { Join-Path $Root $item }
    if (Test-Path -LiteralPath $path) { $dirs += $path }
}
if ($dirs.Count -eq 0) { throw "未找到待检查目录：$($Dir -join ', ')" }

$hits = @()
$missing = @()

foreach ($dir in $dirs) {
    foreach ($file in Get-ChildItem $dir -Recurse -Filter *.cs) {
        $rel = if ($file.FullName.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
            $file.FullName.Substring($Root.Length).TrimStart("\", "/")
        }
        else { $file.FullName }

        if ($rel -match '\\(obj|bin)\\') { continue }
        $excluded = $false
        foreach ($pattern in $Exclude) {
            if ($rel -like $pattern -or $file.FullName -like $pattern) { $excluded = $true; break }
        }
        if ($excluded) { continue }

        $lines = @(Get-Content $file.FullName -Encoding UTF8)
        $inBlock = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i].Trim()

            if ($line -match "/\*") { $inBlock = $true }
            if ($inBlock) {
                if ($line -match "\*/") { $inBlock = $false }
                continue
            }
            if ($line.StartsWith("//")) { continue }

            # 去掉行尾注释，避免把注释里的中文算作运行时消息
            $code = $line -replace "//.*$", ""
            # 特性参数（Description/DisplayName/Obsolete/RequiresXxx/Justification 等）属元数据，不在本检查范围
            if ($code -match "^\s*\[") { continue }
            if ($code -match $MessageCall -and $code -match "[\u4e00-\u9fa5]") {
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $code }
            }

            if ($localeKeys) {
                foreach ($pattern in $keyPatterns) {
                    foreach ($m in [regex]::Matches($code, $pattern)) {
                        $key = Unescape-Yaml $m.Groups[1].Value
                        if ($key -notmatch '[A-Za-z]') { continue }
                        if ($AllowMissing -contains $key) { continue }
                        if (!$localeKeys.Contains($key)) {
                            $missing += [pscustomobject]@{ File = $rel; Line = $i + 1; Key = $key }
                        }
                    }
                }
            }
        }
    }

    # 程序集标题与描述面向 NuGet 页面和关于窗口，统一用英文；PackageDescription 允许中英混排，不在此检查范围
    foreach ($proj in Get-ChildItem $dir -Recurse -Filter *.csproj) {
        $rel = if ($proj.FullName.StartsWith($Root, [StringComparison]::OrdinalIgnoreCase)) {
            $proj.FullName.Substring($Root.Length).TrimStart("\", "/")
        }
        else { $proj.FullName }
        if ($rel -match '\\(obj|bin)\\') { continue }

        $lines = @(Get-Content $proj.FullName -Encoding UTF8)
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $line = $lines[$i]
            if ($line -match '<(?:AssemblyTitle|Description)>(.*?)</(?:AssemblyTitle|Description)>' -and $Matches[1] -match '[\u4e00-\u9fa5]') {
                $hits += [pscustomobject]@{ File = $rel; Line = $i + 1; Text = $line.Trim() }
            }
        }
    }
}

if ($Csv) {
    $hits | Export-Csv -LiteralPath $Csv -NoTypeInformation -Encoding UTF8
    Write-Host "已导出 $($hits.Count) 条中文运行时消息到 $Csv"
    exit 0
}

if ($List) {
    Write-Host "盘点模式：发现 $($hits.Count) 条中文运行时消息" -ForegroundColor Yellow
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 0
}

if ($hits.Count -gt 0) {
    Write-Host "发现 $($hits.Count) 处中文运行时消息，请改为英文：" -ForegroundColor Red
    foreach ($hit in $hits) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Text)
    }
    exit 1
}

if ($missing.Count -gt 0) {
    Write-Host "发现 $($missing.Count) 处运行时消息未登记词表（任何语言下都只能回退英文）：" -ForegroundColor Red
    foreach ($hit in $missing) {
        Write-Host ("  {0}:{1}: {2}" -f $hit.File, $hit.Line, $hit.Key)
    }
    exit 1
}

Write-Host "OK: 未发现中文运行时消息，词表覆盖全部运行时消息。"
exit 0
