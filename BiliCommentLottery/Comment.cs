using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Documents;

namespace BiliCommentLottery
{
    public class Comment : IEquatable<Comment>
    {
        /// <summary>
        /// 用户名
        /// </summary>
        public string UName { get; set; }
        /// <summary>
        /// 用户主页链接
        /// </summary>
        public string SpaceUrl { get; set; }
        /// <summary>
        /// 唯一ID
        /// </summary>
        public string UID { get; set; }
        /// <summary>
        /// 评论链接
        /// </summary>
        public string CommentUrl { get; set; }
        /// <summary>
        /// 评论内容
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 点赞数
        /// </summary>
        public int Like { get; set; }
        /// <summary>
        /// 发表时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 发表时间字符串
        /// </summary>
        public string Timestr { get; set; }
        /// <summary>
        /// UID重复视作重复评论条目
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool Equals(Comment comment)
        {
            return this.UID == comment.UID;
        }
        public override int GetHashCode()
        {
            return UID.GetHashCode();
        }
    }
}
