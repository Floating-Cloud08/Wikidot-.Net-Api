using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
/*
 __        __  _   _      _       _           _           _   _          _          _      ____    ___ 
 \ \      / / (_) | | __ (_)   __| |   ___   | |_        | \ | |   ___  | |_       / \    |  _ \  |_ _|
  \ \ /\ / /  | | | |/ / | |  / _` |  / _ \  | __|       |  \| |  / _ \ | __|     / _ \   | |_) |  | | 
   \ V  V /   | | |   <  | | | (_| | | (_) | | |_     _  | |\  | |  __/ | |_     / ___ \  |  __/   | | 
    \_/\_/    |_| |_|\_\ |_|  \__,_|  \___/   \__|   (_) |_| \_|  \___|  \__|   /_/   \_\ |_|     |___|
   --By FloatingCloud浮云
*/
namespace WikidotNetApi
{
    public class WikidotApi
    {
        string site = "";
        public string url => $"http://{site}.wikidot.com/";
        public string module => $"{url}/ajax-module-connector.php";
        public string username = "";
        public string password = "";
        string token = "wikidot-net-api";
        HttpClient http = new HttpClient();
        public static bool debugMode = true;
        public static int debugPriority = 0;
        public static bool skipIcon = false;
        KeyValuePair<string, string> pageCache = new KeyValuePair<string, string>();
        KeyValuePair<string, Dictionary<string, string>> pageInfoCache = new KeyValuePair<string, Dictionary<string, string>>();
        string get(string url, bool skipCache = false)
        {
            if (!skipCache && pageCache.Key == url)
            {
                debug($"使用缓存获取页面: {url}");
                return pageCache.Value;

            }
            debug($"获取页面: {url}");
            try
            {
                string html = http.GetStringAsync(url).Result;
                pageCache = new KeyValuePair<string, string>(url, html);
                return html;
            }
            catch (Exception ex)
            {
                return $"获取页面失败: {ex.Message}";
            }
        }
        string post(string url, Dictionary<string, string> data)
        {
            var httpContent = new FormUrlEncodedContent(data);
            string result = http.PostAsync(url, httpContent).Result.Content.ReadAsStringAsync().Result;
            debug($"POST请求: {url}\n数据: {JsonSerializer.Serialize(data)}\n", -3);
            debug($"Post返回: {result}\n",-2);
            return Regex.Unescape(result);
        }
        static void Init()
        {
            if (!skipIcon)
            {
                skipIcon = true;
                Console.WriteLine(new String('=', 90));
                Console.WriteLine(@"
 __        __  _   _      _       _           _           _   _          _          _      ____    ___ 
 \ \      / / (_) | | __ (_)   __| |   ___   | |_        | \ | |   ___  | |_       / \    |  _ \  |_ _|
  \ \ /\ / /  | | | |/ / | |  / _` |  / _ \  | __|       |  \| |  / _ \ | __|     / _ \   | |_) |  | | 
   \ V  V /   | | |   <  | | | (_| | | (_) | | |_     _  | |\  | |  __/ | |_     / ___ \  |  __/   | | 
    \_/\_/    |_| |_|\_\ |_|  \__,_|  \___/   \__|   (_) |_| \_|  \___|  \__|   /_/   \_\ |_|     |___|
   --By FloatingCloud浮云
                ");
                Console.WriteLine(@$"全局设置:
启用调试输出:{debugMode}
跳过启动提示:{skipIcon}"
                    );
                Console.WriteLine(new String('=', 90));
            }
        }
        public WikidotApi()
        {
            debug($"初始化...");
            Init();
            http.DefaultRequestHeaders.Add("User-Agent", "WikidotApi");
            http.DefaultRequestHeaders.Add("Cookie", $"wikidot_token7={token}");
            debug($"初始化完成");
        }
        public WikidotApi(string username, string password, string site): this(username,password)
        {
            setSite(site);
        }
        public WikidotApi(string username, string password) : this()
        {
            this.username = username;
            this.password = password;
            login(username, password);
        }
        public void setSite(string site)
        {
            this.site = site;
            debug($"站点已设置为: {site}");
        }
        public string login(string username, string password)
        {
            debug($"登录中...(id:{username},pw:{password})");
            string result = post("https://www.wikidot.com/default--flow/login__LoginPopupScreen", new Dictionary<string, string>() {
                { "action","Login2Action"},
                { "event","login"},
                { "wikidot_token7", token },
                { "login",username},
                { "password",password}
            });
            if (!result.Contains("The login and password do not match."))
            {
                debug($"登录成功");
            }
            else
            {
                debug($"登录失败");
                throw new Exception("Wikidot登录失败");
            }

            return result;
        }
        private DateTime _lastEditTime = DateTime.MinValue;
        void coolDownCheck()
        {
            TimeSpan elapsed = DateTime.Now - _lastEditTime;
            if (elapsed.TotalMilliseconds < 3000)
            {
                int waitMs = (int)(3000 - elapsed.TotalMilliseconds);
                debug($"编辑冷却中，等待 {waitMs}ms...");
                Thread.Sleep(waitMs); // 同步等待，适配你当前的同步代码风格
            }
            _lastEditTime = DateTime.Now;
        }
        public string[] lockEdit(string page)
        {
            debug($"锁定页面{page}编辑锁...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","edit/PageEditModule"},
                { "event","savePage"},
                { "mode","page"},
                { "wiki_page",page},
                { "force_lock","yes"},
            };
            if (getPageInfo(page).TryGetValue("pageId", out string? pageId))
            {
                dic.Add("page_id", pageId);
            }
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                string id = Regex.Match(result, "\"lock_id\":(?<content>[\\d]+),").Groups["content"].Value;
                string key = Regex.Match(result, "\"lock_secret\":\"(?<content>[\\s\\S]*?)\"").Groups["content"].Value;
                string revision = Regex.Match(result, "\"revision_id\":(?<content>[\\d]+),").Groups["content"].Value;
                debug($"页面{page}编辑锁获取成功: id:{id}, key:{key}, revision:{revision}");
                return new string[] { id, key, revision };
            }
            else
            {
                debug($"页面{page}编辑锁移除失败:\n {result}");
                return new string[] { "", "", "" };
            }
        }
        public bool removeEditLock(string page,string lockID,string lockKey)
        {
            debug($"移除页面{page}编辑锁...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "action","WikiPageAction"},
                { "event","removePageEditLock"},
                { "leave_draft","false"},
                { "wiki_page",page},
                { "lock_id", lockID},
                { "lock_secret",lockKey},
                { "page_id",getPageInfo(page)["pageId"]}
            };
            string result = post(module, dic);
            debug("移除锁定结果:"+result);
            return result.Contains("\"status\":\"ok\"");
        }
        public bool pageEditModule(string page) {
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","edit/PageEditModule"},
                { "action","WikiPageAction"},
                { "mode","page"},
                { "wiki_page",page},
                { "page_id",getPageInfo(page)["pageId"]}
                };
                string result = post(module, dic);
            debug(result);
            return result.Contains("\"status\":\"ok\"");
        }
        public bool editPage(string page, string source, string title, string comments = "由Wikidot .net API提交")
        {
            var key = lockEdit(page);
            coolDownCheck();
            debug($"编辑页面{page}...");

            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "event","savePage"},
                { "mode","page"},
                { "wiki_page",page},
                { "source",source},
                { "action","WikiPageAction"},
                { "comments",comments},
                { "title",title},
                { "lock_id",key[0] },
                { "lock_secret",key[1]},
                { "revision_id",key[2]}
            };
            if (getPageInfo(page).TryGetValue("pageId", out string? pageId))
            {
                dic.Add("page_id", pageId);
            }
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                debug($"页面{page}编辑成功");
                debug(result,-1);
                return true;
            }
            else if (result == "")
            {
                debug($"页面{page}无变化");
                return true;
            }
            else if (result.Contains("\"status\":\"need_captcha\""))
            {
                debug($"\n\n触发Wikidot验证码！请用该账号编辑任意页面，填写验证码后再继续脚本。",999);
                debug("按任意键以继续...");
                Console.ReadKey();
                return false;
            }
            else
            {
                debug($"页面{page}编辑失败:\n {result}");
                return false;
                //throw new Exception("Wikidot编辑页面失败");
            }
            //removeEditLock(page, key[0], key[1]);
        }
        public bool editPage(string page, string source, string comments = "由Wikidot .net API提交")
        {
            string title = getPageInfo(page)["title"];
            return editPage(page, source, title, comments);
        }
        public bool deletePage(string page)
        {
            debug($"删除页面{page}...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "event","deletePage"},
                { "action","WikiPageAction"},
                { "page_id",getPageInfo(page)["pageId"]}
            };
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                debug($"页面{page}删除成功");
            }
            else
            {
                debug($"页面{page}删除失败:\n {result}");
            }
            return result.Contains("\"status\":\"ok\"");
        }
        public bool setTag(string page, string tags)
        {
            debug($"向{page}添加标签({tags})...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "event","saveTags"},
                { "action","WikiPageAction"},
                { "pageId",getPageInfo(page)["pageId"]},
                { "tags",tags}
            };
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                debug($"页面{page}标签添加成功");
            }
            else
            {
                debug($"页面{page}标签添加失败:\n {result}");
            }
            return result.Contains("\"status\":\"ok\"");
        }
        public string getPageHtml(string page)
        {
            string html = get(url + page);
            return html;
        }
        static string getHtmlElement(string html, string tag)
        {
            string pattern = $"(?s)<{tag}[^>]*>(?<content>.*?)</{tag}>";
            Match match = Regex.Match(html, pattern);
            if (match.Success)
            {
                return match.Groups["content"].Value;
            }
            return "";
        }
        /// <summary>
        /// 获取页面信息(页面id，页面标题, 网站id等)
        /// </summary>
        /// <param name="page">页面</param>
        /// <returns></returns>
        public Dictionary<string, string> getPageInfo(string page = "", bool skipCache = false)
        {
            debug($"获取页面{page}信息中...");
            var info = new Dictionary<string, string>();
            if (!skipCache && pageInfoCache.Key == page)
            {
                debug($"使用缓存获取页面{page}信息");
                return pageInfoCache.Value;
            }
            string html = getPageHtml(page);
            string head = getHtmlElement(html, "head");
            string title = Regex.Match(html, "<div id=\"requestPage-title\">\\s*(?<content>.*?)\\s*?</div>").Groups["content"].Value;
            info.Add("title", title);
            var pageInfo = Regex.Matches(head, " WIKIREQUEST.info.(?<attr>\\w+)\\s*=\\s*(?<value>.*?)\\s*?;");
            if (pageInfo.Count > 0)
            {
                foreach (Match match in pageInfo)
                {
                    string attr = match.Groups["attr"].Value;
                    string value = match.Groups["value"].Value.Trim('"');
                    info.TryAdd(attr, value);
                }
            }
            debug($"页面{page}信息获取完成");
            pageInfoCache = new KeyValuePair<string, Dictionary<string, string>>(page, info);
            //debug($"页面{requestPage}信息: \n{Regex.Unescape(JsonSerializer.Serialize(info))}");
            return info;
        }
        public bool joinSite(string password)
        {
            debug($"加入站点...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "action","MembershipApplyAction"},
                { "moduleName","membership/MembershipByPasswordResultModule"},
                {"event","applyByPassword" },
                { "password",password}
            };
            string result = post(module, dic);
            bool success = result.Contains("\"status\":\"ok\"");
            if (success)
            {
                debug($"加入站点成功");
            }
            else
            {
                debug($"加入站点失败:\n {result}");
            }
            return success;
        }
        public bool joinSite()
        {
            debug($"加入站点...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "action","MembershipApplyAction"},
                { "moduleName","membership/MembershipByPasswordResultModule"},
                {"event","join" },
            };
            string result = post(module, dic);
            bool success = result.Contains("\"status\":\"ok\"");
            if (success)
            {
                debug($"加入站点成功");
            }
            else
            {
                debug($"加入站点失败:\n {result}");
            }
            return success;
        }
        public bool leaveSite()
        {
            debug($"退出站点...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "action","DashboardSitesAction"},
                { "moduleName","Empty"},
                {"event","memberSignOff" },
                { "site_id", pageInfoCache.Value["siteId"] }
            };
            string result = post("https://www.wikidot.com/ajax-module-connector.php", dic);
            bool success = result.Contains("\"status\":\"ok\"");
            if (success)
            {
                debug($"退出站点成功");
            }
            else
            {
                debug($"退出站点失败:\n {result}");
            }
            return success;
        }
        public string getSourceCode(string page = "")
        {
            debug($"获取源代码...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","viewsource/ViewSourceModule"},
                { "page_id", getPageInfo(page)["pageId"] }
            };
            string result = post(module, dic);
            bool success = result.Contains("\"status\":\"ok\"");
            if (success)
            {
                debug($"获取源代码成功");
                string source = Regex.Match(result, "<div class=\"requestPage-source\">\\s*(?<content>[\\s\\S]*)\\s*</div>").Groups["content"].Value;
                source = source.Replace("<br />", "");
                return source;
            }
            else
            {
                debug($"获取源代码失败:\n {result}");
                return "";
            }
        }
        public class PageHistory
        {
            public PageHistory(string revisionId, string revisionDate, string revisionUser, string revisionComment)
            {
                this.revisionId = revisionId;
                this.revisionDate = revisionDate;
                this.revisionUser = revisionUser;
                this.revisionComment = revisionComment;
            }
            public string revisionId { get; set; }
            public string revisionDate { get; set; }
            public string revisionUser { get; set; }
            public string revisionComment { get; set; }
        }
        public List<PageHistory> getPageHistory(string mode, string requestPage = "", int page = 0, int perPage = 20)
        {
            debug($"获取页面历史...");
            List<PageHistory> historyList = new List<PageHistory>();
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","history/PageRevisionListModule"},
                { "page_id", getPageInfo(requestPage)["pageId"] },
                { "page", page.ToString() },
                { "perpage", perPage.ToString() },
                { "options",mode}//{"all":true,"source":true,"title":true,"move":true,"tags":true,"files":true,"meta":true}
            };
            string result = post(module, dic);
            bool success = result.Contains("\"status\":\"ok\"");
            if (success)
            {
                debug($"获取页面历史成功");
                var source = Regex.Matches(result, "<tr id=\"revision-row-\\d+\">\\s*(?<content>[\\s\\S]*?)\\s*?</tr>");
                foreach (Match match in source)
                {
                    string content = match.Groups["content"].Value;
                    string revisionId = Regex.Match(content, "<td>(?<content>\\d+)\\.</td>").Groups["content"].Value;
                    string revisionDate = Regex.Match(content, "<span class=\"odate.*?>\\s*(?<content>.*?)</span>").Groups["content"].Value;
                    string revisionUser = Regex.Match(content, "<a href=\"http://www\\.wikidot\\.com/user:info/[^\"]*\"[^>]*>(?<name>[^<]*)</a>").Groups["name"].Value;
                    string revisionComment = Regex.Match(content, "<td style=\"font-size: 90%\">(?<content>.*?)</td>").Groups["content"].Value;
                    historyList.Add(new PageHistory(revisionId, revisionDate, revisionUser, revisionComment));
                    debug($"历史记录: id:{revisionId}, date:{revisionDate}, user:{revisionUser}, comment:{revisionComment}");
                }
                return historyList;
            }
            else
            {
                debug($"获取页面历史失败:\n {result}");
                return historyList;
            }
        }
        public List<PageHistory> getPageHistory(string requestPage = "", int page = 0, int perPage = 20, bool all = false, bool source = false, bool title = false, bool move = false, bool tags = false, bool files = false, bool meta = false)
        {
            return getPageHistory($"{{\"all\":{all.ToString().ToLower()},\"source\":{source.ToString().ToLower()},\"title\":{title.ToString().ToLower()},\"move\":{move.ToString().ToLower()},\"tags\":{tags.ToString().ToLower()},\"files\":{files.ToString().ToLower()},\"meta\":{meta.ToString().ToLower()}}}", requestPage, page);
        }
        public bool rate(string page, int rating)
        {
            debug($"向{page}评分({rating})...");
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "event","ratePage"},
                { "action","RateAction"},
                { "force","yes"},
                { "pageId",getPageInfo(page)["pageId"]},
                { "points",rating.ToString()}
            };
            if (rating == 0)
            {
                dic["event"] = "cancelVote";
            }
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                debug($"页面{page}评分成功");
            }
            else
            {
                debug($"页面{page}评分失败:\n {result}");
            }
            return result.Contains("\"status\":\"ok\"");
        }
        public Dictionary<string, string> getWhoRated(string page)
        {
            Dictionary<string, string> users = new Dictionary<string, string>();
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","pagerate/WhoRatedPageModule"},
                { "pageId",getPageInfo(page)["pageId"]}
            };
            string result = post(module, dic);
            if (result.Contains("\"status\":\"ok\""))
            {
                debug($"获取页面评分用户成功");
                var matches = Regex.Matches(result, @"<span class=""printuser avatarhover"">.*?<a href=""http://www\.wikidot\.com/user:info/[^""]*""[^>]*>(?<username>[^<]*)</a>.*?</span>\s*<span style=""color:#777"">\s*(?<symbol>[^<]*?)\s*</span>");
                foreach (Match m in matches)
                {
                    string user = m.Groups["username"].Value;
                    string symbol = m.Groups["symbol"].Value.Trim(); // 去除首尾空白
                    //debug($"用户名: {user}, 符号: {symbol}");
                    users.TryAdd(user, symbol);
                }
            }
            else
            {
                debug($"获取页面评分用户失败:\n {result}");
            }
            return users;
        }
        public bool threadPost(string threadId, string content, string title = "", string reply = "")
        {
            Dictionary<string, string> dic = new Dictionary<string, string>() {
                { "wikidot_token7", token },
                { "moduleName","Empty"},
                { "event","savePost"},
                { "action","ForumAction"},
                { "threadId",threadId},
                {"parentId", reply },
                { "title",title },
                { "source",content}
            };
            string result = post(module, dic);
            return result.Contains("\"status\":\"ok\"");
        }
        public bool threadPostOnPage(string pageId, string content, string title = "", string reply = "")
        {
            string html = getPageHtml(pageId);
            string threadId = Regex.Match(html, "WIKIDOT\\.forumThreadId\\s*?=\\s*(?<value>.*?)\\s*?;").Groups["value"].Value;
            debug($"获取页面{pageId}的论坛主题id: {threadId}");
            return threadPost(threadId, content, title, reply);
        }
        static void debug(object s,int priority = 0)
        {
            if (debugMode && priority >= debugPriority)
            {
                Console.WriteLine(s);
            }
        }
        static void Main(string[] args)
        {
            //Init();
        }
    }
}
