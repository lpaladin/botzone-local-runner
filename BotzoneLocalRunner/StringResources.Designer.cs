﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace BotzoneLocalRunner {
    using System;
    
    
    /// <summary>
    ///   一个强类型的资源类，用于查找本地化的字符串等。
    /// </summary>
    // 此类是由 StronglyTypedResourceBuilder
    // 类通过类似于 ResGen 或 Visual Studio 的工具自动生成的。
    // 若要添加或移除成员，请编辑 .ResX 文件，然后重新运行 ResGen
    // (以 /str 作为命令选项)，或重新生成 VS 项目。
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class StringResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal StringResources() {
        }
        
        /// <summary>
        ///   返回此类使用的缓存的 ResourceManager 实例。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("BotzoneLocalRunner.StringResources", typeof(StringResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   重写当前线程的 CurrentUICulture 属性
        ///   重写当前线程的 CurrentUICulture 属性。
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   查找类似 中止对局时发生错误： 的本地化字符串。
        /// </summary>
        public static string ABORT_FAILED {
            get {
                return ResourceManager.GetString("ABORT_FAILED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Botzone本地AI的URL无效 的本地化字符串。
        /// </summary>
        public static string BAD_LOCALAI_URL {
            get {
                return ResourceManager.GetString("BAD_LOCALAI_URL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 对局组文件的格式有误，无法读取！ 的本地化字符串。
        /// </summary>
        public static string BAD_MATCH_COLLECTION_FORMAT {
            get {
                return ResourceManager.GetString("BAD_MATCH_COLLECTION_FORMAT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 &lt;在此粘贴Botzone本地AI的URL（点击Botzone头像菜单查看）&gt; 的本地化字符串。
        /// </summary>
        public static string BOTZONE_LOCALAI_URL_PROMPT {
            get {
                return ResourceManager.GetString("BOTZONE_LOCALAI_URL_PROMPT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 (https?://.*botzone\.(org|org\.cn))/api/([0-9a-f]+)/([^/]+) 的本地化字符串。
        /// </summary>
        public static string BOTZONE_LOCALAI_URL_REGEX {
            get {
                return ResourceManager.GetString("BOTZONE_LOCALAI_URL_REGEX", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 这是一场 Botzone 评测的对局 的本地化字符串。
        /// </summary>
        public static string BOTZONE_MATCH {
            get {
                return ResourceManager.GetString("BOTZONE_MATCH", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 人类玩家只能和本地AI对战（否则请直接在Botzone网站上进行人机对局） 的本地化字符串。
        /// </summary>
        public static string BOTZONE_MATCH_NO_HUMAN {
            get {
                return ResourceManager.GetString("BOTZONE_MATCH_NO_HUMAN", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 与 Botzone 上的AI对战时必须且只能有一个本地AI 的本地化字符串。
        /// </summary>
        public static string BOTZONE_MATCH_ONE_LOCALAI {
            get {
                return ResourceManager.GetString("BOTZONE_MATCH_ONE_LOCALAI", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 &lt;请在这里输入Bot的ID&gt; 的本地化字符串。
        /// </summary>
        public static string BOTZONEBOT_PLACEHOLDER {
            get {
                return ResourceManager.GetString("BOTZONEBOT_PLACEHOLDER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 请选择游戏 的本地化字符串。
        /// </summary>
        public static string CHOOSE_GAME_FIRST {
            get {
                return ResourceManager.GetString("CHOOSE_GAME_FIRST", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 调用格式不对，需要指明游戏与各个玩家的路径或者ID，用空格分隔 的本地化字符串。
        /// </summary>
        public static string CONSOLE_BAD_FORMAT {
            get {
                return ResourceManager.GetString("CONSOLE_BAD_FORMAT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 参数中的 &quot;{0}&quot; 既不是本地AI的程序路径，也不是Botzone上的ID 的本地化字符串。
        /// </summary>
        public static string CONSOLE_BAD_ID {
            get {
                return ResourceManager.GetString("CONSOLE_BAD_ID", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 该对局必须提供Botzone本地AI的URL才能进行。请提供-u参数。 的本地化字符串。
        /// </summary>
        public static string CONSOLE_BAD_LOCALAI_URL {
            get {
                return ResourceManager.GetString("CONSOLE_BAD_LOCALAI_URL", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 将在Botzone上进行对局，并与本地AI进行交互 的本地化字符串。
        /// </summary>
        public static string CONSOLE_BOTZONEMATCH {
            get {
                return ResourceManager.GetString("CONSOLE_BOTZONEMATCH", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 作者：zhouhy
        ///以下是工具使用帮助——
        ///命令行格式：
        ///	{0} &lt;游戏名&gt; &lt;id-0&gt; &lt;id-1&gt; ... [-u &lt;本地AI的URL&gt;] [-o &lt;对局组路径&gt;] [-l &lt;log路径&gt;]
        ///	{0} -h
        ///
        ///命令行参数帮助：
        ///	&lt;id-i&gt;
        ///		如果玩家i是本地AI，那么写玩家i的本地程序文件路径；如果玩家i是Botzone上的AI，那么写玩家i在Botzone上的BotID。
        ///	-h
        ///		显示此帮助
        ///	-u &lt;本地AI的URL，形如https://www.botzone.org.cn/api/xxx/xxx/localai&gt;
        ///		有Botzone的AI参与时必填。点击Botzone上的头像菜单可以查看本地AI配置，其中就有本地AI的URL，复制过来即可。
        ///	-o &lt;对局组路径，形如xxx.matches&gt;
        ///		如果有该选项，则将该场对局保存到现有对局组或新建的对局组。
        ///	-l &lt;log路径，形如xxx.json&gt;
        ///		如果有该选项，则将该场对局的完整 log 以单行 json 格式保存到文本文件。
        ///	--simple-io
        ///		如果有该选项，则对本地程序使用简单 [字符串的其余部分被截断]&quot;; 的本地化字符串。
        /// </summary>
        public static string CONSOLE_HELP {
            get {
                return ResourceManager.GetString("CONSOLE_HELP", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 正在加载图形界面…… 的本地化字符串。
        /// </summary>
        public static string CONSOLE_LOAD_GUI {
            get {
                return ResourceManager.GetString("CONSOLE_LOAD_GUI", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 将进行本地对局…… 的本地化字符串。
        /// </summary>
        public static string CONSOLE_LOCALMATCH {
            get {
                return ResourceManager.GetString("CONSOLE_LOCALMATCH", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 {0} 选项的后面缺少参数 的本地化字符串。
        /// </summary>
        public static string CONSOLE_MISSING_ARGUMENT {
            get {
                return ResourceManager.GetString("CONSOLE_MISSING_ARGUMENT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 欢迎使用 Botzone 本地调试工具。 的本地化字符串。
        /// </summary>
        public static string CONSOLE_WELCOME {
            get {
                return ResourceManager.GetString("CONSOLE_WELCOME", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 如果不需要图形界面，请添加命令行参数启动。 的本地化字符串。
        /// </summary>
        public static string CONSOLE_WELCOME2 {
            get {
                return ResourceManager.GetString("CONSOLE_WELCOME2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 -h 参数可以查看命令行参数使用方法。 的本地化字符串。
        /// </summary>
        public static string CONSOLE_WELCOME3 {
            get {
                return ResourceManager.GetString("CONSOLE_WELCOME3", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 玩家ID不可为空 的本地化字符串。
        /// </summary>
        public static string ID_EMPTY {
            get {
                return ResourceManager.GetString("ID_EMPTY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 本地AI程序输出不是合法的JSON： 的本地化字符串。
        /// </summary>
        public static string INVALID_JSON {
            get {
                return ResourceManager.GetString("INVALID_JSON", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 这是一场本地运行的对局 的本地化字符串。
        /// </summary>
        public static string LOCAL_MATCH {
            get {
                return ResourceManager.GetString("LOCAL_MATCH", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 本地AI路径不可为空 的本地化字符串。
        /// </summary>
        public static string LOCALAI_PATH_EMPTY {
            get {
                return ResourceManager.GetString("LOCALAI_PATH_EMPTY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 &lt;请使用选择按钮选择程序&gt; 的本地化字符串。
        /// </summary>
        public static string LOCALAI_PLACEHOLDER {
            get {
                return ResourceManager.GetString("LOCALAI_PLACEHOLDER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 &lt;开启对局后在右侧展示画面操作&gt; 的本地化字符串。
        /// </summary>
        public static string LOCALHUMAN_PLACEHOLDER {
            get {
                return ResourceManager.GetString("LOCALHUMAN_PLACEHOLDER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 对局失败： 的本地化字符串。
        /// </summary>
        public static string MATCH_FAILED {
            get {
                return ResourceManager.GetString("MATCH_FAILED", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 对局正在进行，无法回放过往对局。 的本地化字符串。
        /// </summary>
        public static string MATCH_RUNNING_NO_REPLAY {
            get {
                return ResourceManager.GetString("MATCH_RUNNING_NO_REPLAY", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 对局组文件 (*.matches)|*.matches 的本地化字符串。
        /// </summary>
        public static string MATCHES_FILTER {
            get {
                return ResourceManager.GetString("MATCHES_FILTER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 载入对局组 的本地化字符串。
        /// </summary>
        public static string MATCHES_OFD_TITLE {
            get {
                return ResourceManager.GetString("MATCHES_OFD_TITLE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 保存对局组 的本地化字符串。
        /// </summary>
        public static string MATCHES_SFD_TITLE {
            get {
                return ResourceManager.GetString("MATCHES_SFD_TITLE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 提示 的本地化字符串。
        /// </summary>
        public static string MESSAGE {
            get {
                return ResourceManager.GetString("MESSAGE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 不应当有多个对局同时进行！ 的本地化字符串。
        /// </summary>
        public static string NO_PARALLEL_MATCHES {
            get {
                return ResourceManager.GetString("NO_PARALLEL_MATCHES", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 所有程序 (*.exe;*.py;*.js;Main.class)|*.exe;*.py;*.js;*.class|Python 代码 (*.py)|*.py|Node.js 代码 (*.js)|*.js|Java 主类文件 |Main.class 的本地化字符串。
        /// </summary>
        public static string OFD_FILTER {
            get {
                return ResourceManager.GetString("OFD_FILTER", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 选择要运行的本地AI程序 的本地化字符串。
        /// </summary>
        public static string OFD_TITLE {
            get {
                return ResourceManager.GetString("OFD_TITLE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 Botzone 本地调试工具 的本地化字符串。
        /// </summary>
        public static string TITLE {
            get {
                return ResourceManager.GetString("TITLE", resourceCulture);
            }
        }
        
        /// <summary>
        ///   查找类似 人类玩家最多只能有一个 的本地化字符串。
        /// </summary>
        public static string TOO_MANY_HUMAN {
            get {
                return ResourceManager.GetString("TOO_MANY_HUMAN", resourceCulture);
            }
        }
    }
}
