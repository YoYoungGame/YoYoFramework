using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using static UnityEngine.Rendering.VirtualTexturing.Debugging;

namespace YoYo.Asset
{
    /// <summary>
    /// ResourceOwner访问AssetHandle的令牌
    /// </summary>
    public struct HandleToken : IEquatable<HandleToken>
    {
        private HandleBase handle;

        private bool isRawFile;

        private bool valid;

        public HandleToken(HandleBase handle,bool valid, bool isRawFile = false)
        {
            this.handle = handle;
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

        // 重载 == 运算符：比较引用同一性
        public static bool operator ==(HandleToken left, HandleToken right)
        {
            return ReferenceEquals(left.handle, right.handle);
        }

        public static bool operator !=(HandleToken left, HandleToken right)
        {
            return !ReferenceEquals(left.handle, right.handle);
        }

        // 实现 IEquatable<HandleKey>
        public bool Equals(HandleToken other)
        {
            return handle == other.handle && valid == other.valid;
        }

        public override bool Equals(object obj)
        {
            return obj is HandleToken other && Equals(other);
        }

        // 重写 GetHashCode：使用运行时的标识哈希码（基于引用地址）
        public override int GetHashCode()
        {
            return handle == null ? 0 : RuntimeHelpers.GetHashCode(handle);
        }
    }

    /// <summary>
    /// 用户访问资源的令牌
    /// </summary>
    public struct AssetToken
    {
        private HandleToken handle;

        private bool isRawFile;

        private bool valid;

        public AssetToken(HandleToken handle, bool valid, bool isRawFile = false)
        {
            this.handle = handle;
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

        // 重载 == 运算符：比较引用同一性
        public static bool operator ==(AssetToken left, AssetToken right)
        {
            return left.handle == right.handle;
        }

        public static bool operator !=(AssetToken left, AssetToken right)
        {
            return left.handle != right.handle;
        }

        // 实现 IEquatable<HandleKey>
        public bool Equals(AssetToken other)
        {
            return handle.Equals(other.handle);
        }

        public override bool Equals(object obj)
        {
            return obj is AssetToken other && Equals(other);
        }

        // 重写 GetHashCode：使用运行时的标识哈希码（基于引用地址）
        public override int GetHashCode()
        {
            return handle.GetHashCode();
        }
    }
}
