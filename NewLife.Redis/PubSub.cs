using System;

namespace NewLife.Caching
{
    /// <summary>发布订阅</summary>
    public class PubSub : RedisBase
    {
        #region 实例化
        /// <summary>实例化发布订阅</summary>
        /// <param name="redis"></param>
        /// <param name="key"></param>
        public PubSub(Redis redis, String key) : base(redis, key) { }
        #endregion

        /// <summary>订阅给定的模式</summary>
        /// <remarks>
        /// 支持的模式(patterns)有:
        /// h? llo subscribes to hello, hallo and hxllo
        /// h* llo subscribes to hllo and heeeello
        /// h[ae]llo subscribes to hello and hallo, but not hillo
        /// 如果想输入普通的字符，可以在前面添加\
        /// </remarks>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public Int32 PSubscribe(params String[] patterns)
        {
            //var args = new List<Object>
            //{
            //    Key
            //};
            //foreach (var item in patterns)
            //{
            //    args.Add(item);
            //}
            //return Execute(rc => rc.Execute<Int32>("PSUBSCRIBE", args.ToArray()), true);
            return Execute(rc => rc.Execute<Int32>("PSUBSCRIBE", patterns), true);
        }

        /// <summary>指示客户端退订指定模式，若果没有提供模式则退出所有模式</summary>
        /// <returns></returns>
        public Int32 PUnSubscribe(params String[] patterns)
        {
            if (patterns != null && patterns.Length > 0)
                return Execute(rc => rc.Execute<Int32>("PUNSUBSCRIBE", patterns), true);
            else
                return Execute(rc => rc.Execute<Int32>("PUNSUBSCRIBE"), true);
        }

        /// <summary>订阅给指定频道的信息</summary>
        /// <param name="channels"></param>
        /// <returns></returns>
        public Int32 Subscribe(params String[] channels)
        {
            return Execute(rc => rc.Execute<Int32>("SUBSCRIBE", channels), true);
        }

        /// <summary>退订给定的频道</summary>
        /// <returns></returns>
        public Int32 UnSubscribe(params String[] channels)
        {
            if (channels != null && channels.Length > 0)
                return Execute(rc => rc.Execute<Int32>("UNSUBSCRIBE", channels), true);
            else
                return Execute(rc => rc.Execute<Int32>("UNSUBSCRIBE"), true);
        }

        /// <summary>发布</summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Int32 Publish(String message)
        {
            return 0;
        }

        /// <summary>自省</summary>
        /// <returns></returns>
        public Int32 Pubsub()
        {
            return 0;
        }
    }
}