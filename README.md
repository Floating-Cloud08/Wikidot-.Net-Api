# Wikidot .Net Api
一个基于 C#.NET 的 Wikidot API 封装库，支持页面编辑、标签管理、评分系统、历史记录、论坛发帖等核心功能。
| 核心功能 |
| - |
| Wikidot 账号登录 |
| 创建、编辑、删除页面|
| 添加和修改页面标签|
| 页面评分|
| 获取页面修订历史|
| 发帖与回复|
| 自动处理编辑锁|
| 内置缓存机制和自动处理编辑冷却时间|

# 样例代码

```C#
using WikidotNetApi;

// 初始化并登录
var api = new WikidotApi("username", "password", "site");

// 编辑页面
bool success = api.editPage("page-name", "新内容", "标题", "更新说明");
bool delSuccess = deletePage("page-name");//删除页面

// 获取页面信息
var info = api.getPageInfo("page-name");
Console.WriteLine($"页面ID: {info["pageId"]}");

// 页面评分
api.rate("page-name", 1);  // UV
api.rate("page-name", 0);  // NV
api.rate("page-name", -1);  // DV
// 设置标签
api.setTag("page-name", "标签1 标签2");

```


## 静态配置字段

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `debugMode` | `bool` | `true` | 是否启用调试输出 |
| `debugPriority` | `int` | `0` | 调试信息输出优先级，数值越低越详细 |
| `skipIcon` | `bool` | `false` | 是否跳过启动时的 ASCII 图标打印 |

---

## 构造函数

| 构造函数 | 参数 | 说明 |
|----------|------|------|
| `WikidotApi()` | 无 | 创建一个未登录的实例，需手动调用 `login` 和 `setSite` |
| `WikidotApi(string username, string password)` | `username` 用户名<br>`password` 密码 | 创建实例并自动调用 `login` 登录 |
| `WikidotApi(string username, string password, string site)` | `username` 用户名<br>`password` 密码<br>`site` 站点名（不含 `.wikidot.com`） | 创建实例、登录并设置目标站点 |

---

## 站点与会话

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `setSite` | `void setSite(string site)` | `void` | 设置当前操作的 Wikidot 站点（如 `"my-site"`） |
| `login` | `string login(string username, string password)` | `string` | 登录 Wikidot，返回登录响应内容；失败时抛出异常 |
| `joinSite` | `bool joinSite(string password)` | `bool` | 通过密码加入当前站点 |
| `joinSite` | `bool joinSite()` | `bool` | 加入无需密码的开放站点 |
| `leaveSite` | `bool leaveSite()` | `bool` | 退出当前站点 |

---

## 页面操作

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `getPageHtml` | `string getPageHtml(string page)` | `string` | 获取指定页面的完整 HTML 源码 |
| `getPageInfo` | `Dictionary<string, string> getPageInfo(string page = "", bool skipCache = false)` | `Dictionary<string, string>` | 获取页面元信息（如 `pageId`、`title`、`siteId` 等） |
| `getSourceCode` | `string getSourceCode(string page = "")` | `string` | 获取页面的 Wikidot 源代码（纯文本） |
| `editPage` | `bool editPage(string page, string source, string title, string comments = "由Wikidot .net API提交")` | `bool` | 编辑页面（需指定标题） |
| `editPage` | `bool editPage(string page, string source, string comments = "由Wikidot .net API提交")` | `bool` | 编辑页面（自动使用当前标题） |
| `deletePage` | `bool deletePage(string page)` | `bool` | 删除页面 |
| `pageEditModule` | `bool pageEditModule(string page)` | `bool` | 触发页面编辑模块（一般用于测试） |

---

## 标签操作

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `setTag` | `bool setTag(string page, string tags)` | `bool` | 设置页面标签（多个标签用空格分隔） |

---

## 评分操作

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `rate` | `bool rate(string page, int rating)` | `bool` | 对页面评分，`rating` 为 1~5 表示星级，`0` 表示取消评分 |
| `getWhoRated` | `Dictionary<string, string> getWhoRated(string page)` | `Dictionary<string, string>` | 获取评分用户列表，返回 `用户名 -> 评分符号` 的字典 |

---

## 编辑锁操作

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `lockEdit` | `string[] lockEdit(string page)` | `string[]` | 编辑页面锁定，返回数组 `[lockId, lockSecret, revisionId]` |
| `removeEditLock` | `bool removeEditLock(string page, string lockID, string lockKey)` | `bool` | 移除自己的编辑锁 |

---

## 历史记录

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `getPageHistory` | `List<PageHistory> getPageHistory(string mode, string requestPage = "", int page = 0, int perPage = 20)` | `List<PageHistory>` | 获取页面修订历史，`mode` 为 JSON 字符串指定筛选条件 |
| `getPageHistory` | `List<PageHistory> getPageHistory(string requestPage = "", int page = 0, int perPage = 20, bool all = false, bool source = false, bool title = false, bool move = false, bool tags = false, bool files = false, bool meta = false)` | `List<PageHistory>` | 便捷重载，自动构造筛选 JSON |

---

## 论坛操作

| 方法 | 签名 | 返回类型 | 说明 |
|------|------|----------|------|
| `threadPost` | `bool threadPost(string threadId, string content, string title = "", string reply = "")` | `bool` | 在指定论坛主题下发帖；`reply` 为父帖 ID 时表示回复 |
| `threadPostOnPage` | `bool threadPostOnPage(string pageId, string content, string title = "", string reply = "")` | `bool` | 在页面关联的论坛主题下发帖（自动获取 threadId） |

---

## 内部数据类

### `PageHistory`

| 属性 | 类型 | 说明 |
|------|------|------|
| `revisionId` | `string` | 修订版本号 |
| `revisionDate` | `string` | 修订日期 |
| `revisionUser` | `string` | 修订用户 |
| `revisionComment` | `string` | 修订备注 |

---

# 适用场景
- 批量化编辑页面
- 刷Kama
