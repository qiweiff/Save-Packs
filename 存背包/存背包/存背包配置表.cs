using Newtonsoft.Json;
using TShockAPI;

namespace 存背包
{
    public class 存背包配置表
    {
        public static void GetConfig()
        {
            try
            {
                if (!File.Exists(path))
                {
                    FileTools.CreateIfNot(Path.Combine(TShock.SavePath, "存背包配置表.json"), JsonConvert.SerializeObject(存背包.配置, Formatting.Indented));
                    存背包.配置 = JsonConvert.DeserializeObject<存背包配置表>(File.ReadAllText(Path.Combine(TShock.SavePath, "存背包配置表..json")));
                    File.WriteAllText(path, JsonConvert.SerializeObject(存背包.配置, Formatting.Indented));
                }
            }
            catch
            {
                TSPlayer.Server.SendErrorMessage($"[称号插件]配置文件读取错误！！！");
            }
        }
        public List<数据> 保存的背包 = new() { };
        public class 数据
        {
            public int 背包ID = 0;
            public string 背包备注 = "无备注";
            public int 血量 = 100;
            public int 蓝量 = 20;
            public int 渔夫任务 = 0;
            public bool 恶魔之心 = false;
            public bool 火把神 = false;
            public bool 工匠面包 = false;
            public bool 生命水晶 = false;
            public bool 埃癸斯果 = false;
            public bool  奥术水晶 = false;
            public bool 银河珍珠 = false;
            public bool 黏性蠕虫 = false;
            public bool 珍馐 = false;
            public bool 矿车升级包 = false;
            public List<Inventory> 背包 = new() { };
        }


        public static string path = "tshock/存背包配置表.json";
        public class Inventory
        {
            public int 格子 = 0;
            public int 物品 = 0;
            public int 前缀 = 0;
            public int 数量 = 0;
        }
    }
}

