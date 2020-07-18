using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BiliCommentLottery
{
    /// <summary>
    /// CommentFilter.xaml 的交互逻辑
    /// </summary>
    public partial class CommentFilter : Window
    {
        /// <summary>
        /// 抽奖页面数量
        /// </summary>
        public static int count = 0;
        private ObservableCollection<Comment> CommentList = new ObservableCollection<Comment>();
        /// <summary>
        /// 评论区ID
        /// </summary>
        private string OID;
        /// <summary>
        /// 作品类型
        /// 1: 视频
        /// 12: 专栏
        /// 11&17: 动态
        /// </summary>
        private int WorkType;
        /// <summary>
        /// 评论链接前缀
        /// </summary>
        private string CommentURL;
        /// <summary>
        /// 评论楼层数
        /// </summary>
        private int n_comment;
        /// <summary>
        /// 预定中奖人数
        /// </summary>
        private int n_roll;
        /// <summary>
        /// 当前显示的是抽奖结果
        /// </summary>
        private bool showRolled = false;
        /// <summary>
        /// 初始化评论列表
        /// </summary>
        /// <param name="ID">友好的AV/BV/CV或动态号</param>
        public CommentFilter(string ID)
        {
            if (!ValidID(ID))
            {
                this.Close();
                return;
            }
            InitializeComponent();
            // 小时选择列表
            List<int> list_hours = new List<int>();
            for (int i = 0; i < 24; ++i)
            {
                list_hours.Add(i);
            }
            cmb_StartHour.ItemsSource = list_hours;
            cmb_StopHour.ItemsSource = list_hours;
            // 分钟选择列表
            List<int> list_minutes = new List<int>();
            for (int i = 0; i < 60; ++i)
            {
                list_minutes.Add(i);
            }
            cmb_StartMinute.ItemsSource = list_minutes;
            cmb_StopMinute.ItemsSource = list_minutes;
            CommentGrid.DataContext = CommentList;
            this.Title += " - " + ID;
            ++count;
        }
        /// <summary>
        /// 验证给出的av/bv/cv/动态号是否有效
        /// </summary>
        /// <param name="ID">av/bv/cv/动态号</param>
        /// <returns>
        /// true:       有效
        /// false:      无效
        /// </returns>
        private bool ValidID(string ID)
        {
            string lower_head;
            if (ID.Length > 2)
            {
                lower_head = ID.Substring(0, 2).ToLower();
            }
            else
            {
                lower_head = ID.ToLower();
            }
            // 作品信息接口地址
            string info_url;
            bool valid = false;
            switch (lower_head)
            {
                case "av":
                case "bv":
                    WorkType = 1;
                    CommentURL = "https://www.bilibili.com/video/" + ID + "#reply";
                    info_url = (lower_head == "av")
                        ? "https://api.bilibili.com/x/web-interface/archive/stat?aid=" + ID.Substring(2)
                        : "https://api.bilibili.com/x/web-interface/archive/stat?bvid=" + ID.Substring(2);
                    break;
                case "cv":
                    WorkType = 12;
                    CommentURL = "https://www.bilibili.com/read/" + ID + "#reply";
                    info_url = "https://api.bilibili.com/x/article/viewinfo?id=" + ID.Substring(2);
                    break;
                default:
                    WorkType = 17;
                    CommentURL = "https://t.bilibili.com/" + ID + "#reply";
                    info_url = "https://api.vc.bilibili.com/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id=" + ID;
                    break;
            }
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(info_url);
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                using (Stream respstream = webResponse.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(respstream))
                    {
                        string ret = streamReader.ReadToEnd();
                        if (ret.StartsWith("{\"code\":0,"))
                        {
                            // 专栏稿件直接使用cv号的数字部分作为oid
                            if (WorkType == 12)
                            {
                                OID = ID.Substring(2);
                                valid = true;
                            }
                            else
                            {
                                var top = JsonSerializer.Deserialize<Dictionary<string, object>>(ret);
                                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                                // 动态需要判断是否存在rid
                                if (WorkType == 17)
                                {
                                    if (data.ContainsKey("card"))
                                    {
                                        var card = JsonSerializer.Deserialize<Dictionary<string, object>>(data["card"].ToString());
                                        var desc = JsonSerializer.Deserialize<Dictionary<string, object>>(card["desc"].ToString());
                                        // get_dynamic_detail中type为4: WorkType=17, OID=动态ID
                                        if (desc["type"].ToString() == "4")
                                        {
                                            WorkType = 17;
                                            OID = ID;
                                        }
                                        // get_dynamic_detail中type为2: WorkType=11, OID=rid
                                        else if (desc["type"].ToString() == "2")
                                        {
                                            WorkType = 11;
                                            OID = desc["rid"].ToString();
                                        }
                                        valid = true;
                                    }
                                }
                                // 视频稿件使用aid作为oid
                                else if (WorkType == 1)
                                {
                                    OID = data["aid"].ToString();
                                    valid = true;
                                }
                            }
                        }
                    }
                }
            }
            return valid;
        }
        /// <summary>
        /// 获取所有评论（不包含楼中楼）
        /// </summary>
        /// <returns>评论数（不包含楼中楼）</returns>
        private int GetComments()
        {
            int n_comment = 0;
            int n_page = int.MaxValue;
            string replyapiURL = "https://api.bilibili.com/x/v2/reply?oid=" + OID + "&type=" + WorkType.ToString() + "&sort=0&pn=";
            for (int i = 1; i <= n_page; ++i)
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(replyapiURL + i.ToString());
                try
                {
                    using (WebResponse webResponse = webRequest.GetResponse())
                    {
                        using (Stream respstream = webResponse.GetResponseStream())
                        {
                            using (StreamReader streamReader = new StreamReader(respstream))
                            {
                                string ret = streamReader.ReadToEnd();
                                var top = JsonSerializer.Deserialize<Dictionary<string, object>>(ret);
                                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(top["data"].ToString());
                                var page = JsonSerializer.Deserialize<Dictionary<string, int>>(data["page"].ToString());
                                if (i == 1)
                                {
                                    n_comment = page["count"];
                                    n_page = (int)Math.Ceiling(n_comment / 20.0);
                                }
                                var replies = JsonSerializer.Deserialize<IList>(data["replies"].ToString());
                                foreach (var o_reply in replies)
                                {
                                    Comment comment = new Comment();
                                    var reply = JsonSerializer.Deserialize<Dictionary<string, object>>(o_reply.ToString());
                                    comment.CommentUrl = CommentURL + reply["rpid_str"].ToString();
                                    comment.Time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(long.Parse(reply["ctime"].ToString())).ToLocalTime();
                                    comment.Timestr = comment.Time.ToString("yyyy-MM-dd HH:mm:ss");
                                    comment.Like = int.Parse(reply["like"].ToString());
                                    var member = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["member"].ToString());
                                    comment.UID = member["mid"].ToString();
                                    comment.UName = member["uname"].ToString();
                                    comment.SpaceUrl = "https://space.bilibili.com/" + comment.UID;
                                    var content = JsonSerializer.Deserialize<Dictionary<string, object>>(reply["content"].ToString());
                                    comment.Message = content["message"].ToString();
                                    CommentList.Add(comment);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString(), "Error");
                    break;
                }
                finally
                {
                    if (webRequest != null)
                    {
                        webRequest.Abort();
                    }
                }
            }
            
            return n_comment;
        }
        /// <summary>
        /// 单击列表中UID按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UID_Click(object sender, RoutedEventArgs e)
        {
            string url = ((Button)sender).Tag.ToString();
            Clipboard.SetDataObject(url);
            MessageBox.Show("用户主页链接：\n" + url + "\n已复制到剪切板");
        }
        /// <summary>
        /// 单击列表中评论按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Comment_Click(object sender, RoutedEventArgs e)
        {
            string url = ((Button)sender).Tag.ToString();
            Clipboard.SetDataObject(url);
            MessageBox.Show("评论链接：\n" + url + "\n已复制到剪切板");
        }
        /// <summary>
        /// 窗体加载完毕后初始化评论列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    n_comment = GetComments();
                    lbl_comment.Content = n_comment;
                    roll.IsEnabled = true;
                });
            });
        }
        /// <summary>
        /// 关闭抽奖页面后计数-1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            --count;
        }
        /// <summary>
        /// 单击展示抽奖结果/返回所有楼层评论
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void roll_Click(object sender, RoutedEventArgs e)
        {
            if (showRolled)
            {
                CommentGrid.DataContext = CommentList;
                showRolled = false;
                roll.Content = "展示抽奖结果";
            }
            else
            {
                // 检查预定中奖人数
                if (int.TryParse(txt_nroll.Text, out n_roll) && n_roll <= n_comment && n_roll > 0)
                {
                    IEnumerable<Comment> ECList = CommentList.AsEnumerable();
                    // 检查是否筛选特定评论
                    if (chk_specomment.IsChecked.Value)
                    {
                        ECList = ECList.Where(c => c.Message == txt_specomment.Text);
                    }
                    // 检查是否去重
                    if (chk_distinct.IsChecked.Value)
                    {
                        ECList = ECList.Distinct();
                    }
                    // 检查时间界限
                    DateTime start = DateTime.MinValue;
                    if (!chk_unlimitstart.IsChecked.Value)
                    {
                        start = date_start.SelectedDate.Value
                                .AddMinutes(cmb_StartMinute.SelectedIndex)
                                .AddHours(cmb_StartHour.SelectedIndex);
                        if (date_start.SelectedDate != null)
                        {
                            if (chk_eqstart.IsChecked.Value)
                            {
                                ECList = ECList.Where(c => c.Time >= start);
                            }
                            else
                            {
                                ECList = ECList.Where(c => c.Time > start);
                            }
                        }
                        else
                        {
                            MessageBox.Show("请选择开始时间日期！");
                            return;
                        }
                    }
                    // 检查截止时间界限
                    if (!chk_unlimitstop.IsChecked.Value)
                    {
                        if (date_stop.SelectedDate != null)
                        {
                            DateTime stop = date_stop.SelectedDate.Value
                                .AddMinutes(cmb_StopMinute.SelectedIndex)
                                .AddHours(cmb_StopHour.SelectedIndex);
                            if (start < stop)
                            {
                                if (chk_eqstop.IsChecked.Value)
                                {
                                    ECList = ECList.Where(c => c.Time <= stop);
                                }
                                else
                                {
                                    ECList = ECList.Where(c => c.Time < stop);
                                }
                            }
                            else
                            {
                                MessageBox.Show("截止时间不可小于或等于开始时间！\n请调整开始时间或截止时间");
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("请选择结束时间日期！");
                            return;
                        }
                    }
                    // 检查初步筛选后个数
                    int n_filted = ECList.Count();
                    if (n_filted < n_roll)
                    {
                        MessageBox.Show("根据条件初步筛选后评论数少于预定中奖人数！\n请调整筛选条件或中奖人数");
                    }
                    else
                    {
                        ObservableCollection<Comment> tComments = new ObservableCollection<Comment>(ECList);
                        ObservableCollection<Comment> result = new ObservableCollection<Comment>();
                        Random random = new Random();
                        for (int i = n_filted - 1; i >= n_filted - n_roll; --i)
                        {
                            int pRandom = random.Next(0, i);
                            result.Add(tComments.ElementAt(pRandom));
                            tComments[pRandom] = tComments[i];
                        }
                        CommentGrid.DataContext = result;
                        showRolled = true;
                        roll.Content = "返回所有楼层评论";
                        MessageBox.Show("抽奖完成！");
                    }
                }
                else
                {
                    MessageBox.Show("评论条数少于预定中奖人数！\n请输入范围在 1 ~ " + n_comment.ToString() + " 的正确整数");
                }
            }
        }
    }
}
