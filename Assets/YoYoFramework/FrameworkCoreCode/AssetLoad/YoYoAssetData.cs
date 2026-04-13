#if YoYo_AssetModule
using System;
using System.Runtime.CompilerServices;
using YooAsset;

namespace YoYo.Asset
{
    /// <summary>
    /// 用户访问资源的令牌
    /// </summary>
    public struct AssetToken
    {
        internal string location;

        internal bool isRawFile;

        internal bool valid;

        internal AssetToken(string location, bool valid, bool isRawFile = false)
        {
            this.location = location;
            this.valid = valid;
            this.isRawFile = isRawFile;
        }

        public bool GetValid()
        {
            return valid;
        }

        public bool GetIsRawFile()
        {
            return isRawFile;
        }

        public string GetLocation()
        {
            return location;
        }
    }
}
#endif