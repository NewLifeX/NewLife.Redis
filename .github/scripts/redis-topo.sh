#!/bin/sh
# Redis 集群与哨兵拓扑：集群 7000-7002 / 哨兵主 7480 / 哨兵 26379（6379 由调用方先行提供）
# 由 aot.yml 在 Linux（ubuntu-latest / ubuntu-24.04-arm）复用；QEMU 用户态仿真下宿主回环网络对仿真进程可见
#
# 阻止 apt 安装时自动启动 redis 服务（6379 已由调用方占用）
printf '#!/bin/sh\nexit 101\n' | sudo tee /usr/sbin/policy-rc.d > /dev/null
sudo chmod +x /usr/sbin/policy-rc.d
sudo apt-get update -qq
sudo apt-get install -y -qq redis-server redis-tools
sudo rm -f /usr/sbin/policy-rc.d

# 3 节点集群
for p in 7000 7001 7002; do
  mkdir -p /tmp/aot-redis/c$p
  cat > /tmp/aot-redis/c$p/redis.conf <<EOF
port $p
cluster-enabled yes
cluster-config-file nodes.conf
cluster-node-timeout 5000
appendonly no
save ""
logfile /tmp/aot-redis/c$p/redis.log
dir /tmp/aot-redis/c$p
EOF
  redis-server /tmp/aot-redis/c$p/redis.conf --daemonize yes
done

# 等待节点就绪后建集群，并确认集群状态
for i in $(seq 1 30); do redis-cli -p 7000 ping | grep -q PONG && break; sleep 1; done
redis-cli --cluster create 127.0.0.1:7000 127.0.0.1:7001 127.0.0.1:7002 --cluster-replicas 0 --cluster-yes
for i in $(seq 1 30); do redis-cli -p 7000 cluster info | grep -q 'cluster_state:ok' && break; sleep 1; done
redis-cli -p 7000 cluster info | grep -q 'cluster_state:ok'

# 哨兵：主 7480 + 哨兵 26379
mkdir -p /tmp/aot-redis/master /tmp/aot-redis/sentinel
cat > /tmp/aot-redis/master/redis.conf <<EOF
port 7480
appendonly no
save ""
logfile /tmp/aot-redis/master/redis.log
dir /tmp/aot-redis/master
EOF
redis-server /tmp/aot-redis/master/redis.conf --daemonize yes
cat > /tmp/aot-redis/sentinel/sentinel.conf <<EOF
port 26379
dir /tmp/aot-redis/sentinel
logfile /tmp/aot-redis/sentinel/sentinel.log
sentinel monitor mymaster 127.0.0.1 7480 1
sentinel down-after-milliseconds mymaster 5000
EOF
redis-server /tmp/aot-redis/sentinel/sentinel.conf --sentinel --daemonize yes
for i in $(seq 1 30); do redis-cli -p 26379 sentinel get-master-addr-by-name mymaster | grep -q 7480 && break; sleep 1; done
redis-cli -p 26379 sentinel get-master-addr-by-name mymaster | grep -q 7480

echo "拓扑就绪：集群 7000-7002（cluster_state:ok），哨兵 26379 → 主 7480"
