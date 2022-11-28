using Newtonsoft.Json;
using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using static 存背包.存背包配置表;

namespace 存背包
{
    [ApiVersion(2, 1)]//api版本
    public class 存背包 : TerrariaPlugin
    {
        /// 插件作者
        public override string Author => "奇威复反";
        /// 插件说明
        public override string Description => "保存和读取背包";
        /// 插件名字
        public override string Name => "存背包";
        /// 插件版本
        public override Version Version => new(1, 3, 0, 0);
        /// 插件处理
        public 存背包(Main game) : base(game)
        {
        }
        //插件启动时，用于初始化各种狗子
        public static 存背包配置表 配置 = new();
        public List<查> 状态 = new();
        public class 查
        {
            public string 玩家名 = "";
            public List<存背包配置表.数据> 原背包 = new();
        }
        public static string path = "tshock/存背包配置表.json";
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);//钩住游戏初始化时
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            存背包配置表.GetConfig();
            Reload();
        }
        /// 插件关闭时
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Deregister hooks here
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);//销毁游戏初始化狗子
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);

            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)//游戏初始化的狗子
        {
            //第一个是权限，第二个是子程序，第三个是指令
            Commands.ChatCommands.Add(new Command("存背包.保存", 指令1, "保存背包") { });
            Commands.ChatCommands.Add(new Command("存背包.读取", 指令2, "读取背包") { });
            Commands.ChatCommands.Add(new Command("存背包.删除", 指令3, "删除背包") { });
            Commands.ChatCommands.Add(new Command("存背包.重载", 重载, "reload") { });

        }

        private void 指令1(CommandArgs args)
        {
            try
            {
                if (args.Parameters.Count == 0)
                {
                    args.Player.SendInfoMessage($"正确指令：/保存背包 玩家名 <备注>");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 背包编号");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 <返回/确定>");
                    args.Player.SendInfoMessage($"正确指令：/删除背包 背包编号");
                    args.Player.SendInfoMessage($"背包列表：\n");
                    foreach (var z in 配置.保存的背包)
                    {

                        args.Player.SendInfoMessage($"[{z.背包ID}]{z.背包备注}\n");
                    }
                    return;
                }
                foreach (var plr in TShock.Players)
                {
                    if (plr.Name == args.Parameters[0])
                    {
                        List<数据> pack = SavePack(plr);
                        配置.保存的背包.Add(new() { });
                        int L = 配置.保存的背包.Count - 1;
                        配置.保存的背包[L] = pack[0];
                        配置.保存的背包[L].背包ID = L;
                        if (args.Parameters.Count >= 2)
                        {
                            配置.保存的背包[L].背包备注 = args.Parameters[1];
                        }
                        args.Player.SendInfoMessage($"[存背包]保存[c/FF3333:{args.Parameters[0]}]的背包成功！");
                        File.WriteAllText(path, JsonConvert.SerializeObject(配置, Formatting.Indented));
                        return;
                    }
                }
                args.Player.SendInfoMessage($"[存背包]没有找到玩家\"{args.Parameters[0]}\"");
            }
            catch
            {
                args.Player.SendErrorMessage($"[存背包]保存背包失败！");
            }
        }

        private void 指令2(CommandArgs args)
        {
            try
            {
                if (args.Parameters.Count == 0)
                {
                    args.Player.SendInfoMessage($"正确指令：/保存背包 玩家名 <备注>");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 背包编号");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 <返回/确定>");
                    args.Player.SendInfoMessage($"正确指令：/删除背包 背包编号");
                    args.Player.SendInfoMessage($"背包列表：\n");
                    foreach (var z in 配置.保存的背包)
                    {

                        args.Player.SendInfoMessage($"[{z.背包ID}]{z.背包备注}\n");
                    }
                    return;
                }
                if (args.Parameters[0] == "返回" || args.Parameters[0] == "取消")
                {
                    if (状态.Exists(s => s.玩家名 == args.Player.Name))//确认玩家是否读取其他背包
                    {
                        var b = 状态.Find(s => s.玩家名 == args.Player.Name);
                        if (b != null)
                        {
                            WritePack(args.Player, b.原背包[0]);
                            args.Player.SendInfoMessage($"[存背包]成功返回原背包");
                            状态.RemoveAll(s => s.玩家名 == args.Player.Name);
                        }
                        return;
                    }
                    else
                    {
                        args.Player.SendInfoMessage($"[存背包]您没有读取其他背包");
                        return;
                    }
                }
                if (args.Parameters[0] == "确定")
                {
                    if (状态.Exists(s => s.玩家名 == args.Player.Name))
                    {
                        状态.RemoveAll(s => s.玩家名 == args.Player.Name);
                        args.Player.SendInfoMessage($"[存背包]确定成功！当前背包为您的背包");
                        return;
                    }
                    else
                    {
                        args.Player.SendInfoMessage($"[存背包]您没有读取其他背包");
                        return;
                    }
                }
                if (配置.保存的背包.Exists(s => s.背包ID == Convert.ToInt32(args.Parameters[0])))
                {
                    var z = 配置.保存的背包.Find(s => s.背包ID == Convert.ToInt32(args.Parameters[0]));
                    if (z == null)
                    {
                        args.Player.SendErrorMessage($"[存背包]读取背包失败");
                        return;
                    }
                    if (!状态.Exists(s => s.玩家名 == args.Player.Name))//确认玩家是否读取其他背包
                    {
                        var ybb = SavePack(args.Player);
                        状态.Add(new() { 玩家名 = args.Player.Name, 原背包 = ybb });
                    }
                    WritePack(args.Player, z);
                    args.Player.SendInfoMessage($"[存背包]读取ID为[c/FF3333:{args.Parameters[0]}]的背包成功！");
                }
                else
                {
                    args.Player.SendInfoMessage($"[存背包]没有找到ID为[c/FF3333:{args.Parameters[0]}]的背包");
                }

            }
            catch
            {
                args.Player.SendErrorMessage($"[存背包]读取背包失败！");
            }
        }
        private void 指令3(CommandArgs args)
        {
            try
            {
                if (args.Parameters.Count == 0)
                {
                    args.Player.SendInfoMessage($"正确指令：/保存背包 玩家名 <备注>");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 背包编号");
                    args.Player.SendInfoMessage($"正确指令：/读取背包 <返回/确定>");
                    args.Player.SendInfoMessage($"正确指令：/删除背包 背包编号");
                    args.Player.SendInfoMessage($"背包列表：\n");
                    foreach (var z in 配置.保存的背包)
                    {

                        args.Player.SendInfoMessage($"[{z.背包ID}]{z.背包备注}\n");
                    }
                    return;
                }
                if (配置.保存的背包.Exists(s => s.背包ID == Convert.ToInt32(args.Parameters[0])))
                {
                    var z = 配置.保存的背包.Find(s => s.背包ID == Convert.ToInt32(args.Parameters[0]));
                    if (z == null)
                    {
                        args.Player.SendErrorMessage($"[存背包]删除背包失败");
                        return;
                    }
                    配置.保存的背包.RemoveAll(s => s.背包ID == Convert.ToInt32(args.Parameters[0]));
                    File.WriteAllText(path, JsonConvert.SerializeObject(配置, Formatting.Indented));
                    Reload();
                    args.Player.SendInfoMessage($"[存背包]删除ID为[c/FF3333:{args.Parameters[0]}]的背包成功！");
                    args.Player.SendInfoMessage($"新的背包列表：\n");
                    foreach (var z2 in 配置.保存的背包)
                    {

                        args.Player.SendInfoMessage($"[{z2.背包ID}]{z2.背包备注}\n");
                    }
                    return;
                }
                else
                {
                    args.Player.SendInfoMessage($"[存背包]没有找到ID为[c/FF3333:{args.Parameters[0]}]的背包");
                }

            }
            catch
            {
                args.Player.SendErrorMessage($"[存背包]删除背包失败！");
            }
        }
        private void OnLeave(LeaveEventArgs args)
        {

            if (状态.Exists(s => s.玩家名 == TShock.Players[args.Who].Name))
            {
                var b = 状态.Find(s => s.玩家名 == TShock.Players[args.Who].Name);
                if (b != null)
                {
                    WritePack(TShock.Players[args.Who], b.原背包[0]);
                    状态.RemoveAll(s => s.玩家名 == TShock.Players[args.Who].Name);
                }
            }
            else
            {
            }
        }
        public static List<数据> SavePack(TSPlayer plr)//读取指定玩家的背包，玩家需要在线
        {
            try
            {
                List<Inventory> 背包 = new() { };
                List<数据> 增益 = new() { };
                for (int i = 0; i < 59; i++)
                {
                    Item tritem = plr.TPlayer.inventory[i];
                    if (tritem.netID != 0)
                    {
                        var netID = tritem.netID;
                        var stack = tritem.stack;
                        var prefix = tritem.prefix;
                        背包.Add(new()
                        {
                            格子 = i,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int j = 0; j < NetItem.ArmorSlots; j++)
                {
                    Item tritem2 = plr.TPlayer.armor[j];
                    if (tritem2.netID != 0)
                    {
                        var netID = tritem2.netID;
                        var stack = tritem2.stack;
                        var prefix = tritem2.prefix;
                        背包.Add(new()
                        {
                            格子 = j + 59,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int k = 0; k < NetItem.DyeSlots; k++)
                {
                    Item tritem3 = plr.TPlayer.dye[k];
                    if (tritem3.netID != 0)
                    {
                        var netID = tritem3.netID;
                        var stack = tritem3.stack;
                        var prefix = tritem3.prefix;
                        背包.Add(new()
                        {
                            格子 = k + 79,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int l = 0; l < NetItem.MiscEquipSlots; l++)
                {
                    Item tritem4 = plr.TPlayer.miscEquips[l];
                    if (tritem4.netID != 0)
                    {
                        var netID = tritem4.netID;
                        var stack = tritem4.stack;
                        var prefix = tritem4.prefix;
                        背包.Add(new()
                        {
                            格子 = l + 89,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int m = 0; m < NetItem.MiscDyeSlots; m++)
                {
                    Item tritem5 = plr.TPlayer.miscDyes[m];
                    if (tritem5.netID != 0)
                    {
                        var netID = tritem5.netID;
                        var stack = tritem5.stack;
                        var prefix = tritem5.prefix;
                        背包.Add(new()
                        {
                            格子 = m + 94,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int n = 0; n < NetItem.PiggySlots; n++)
                {
                    Item tritem6 = plr.TPlayer.bank.item[n];
                    if (tritem6.netID != 0)
                    {
                        var netID = tritem6.netID;
                        var stack = tritem6.stack;
                        var prefix = tritem6.prefix;
                        背包.Add(new()
                        {
                            格子 = n + 99,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int i2 = 0; i2 < NetItem.SafeSlots; i2++)
                {
                    Item tritem7 = plr.TPlayer.bank2.item[i2];
                    if (tritem7.netID != 0)
                    {
                        var netID = tritem7.netID;
                        var stack = tritem7.stack;
                        var prefix = tritem7.prefix;
                        背包.Add(new()
                        {
                            格子 = i2 + 139,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                背包.Add(new()
                {
                    格子 = 179,
                    物品 = plr.TPlayer.trashItem.netID,
                    数量 = plr.TPlayer.trashItem.stack,
                    前缀 = plr.TPlayer.trashItem.prefix
                });
                for (int i3 = 0; i3 < NetItem.ForgeSlots; i3++)
                {
                    Item tritem8 = plr.TPlayer.bank3.item[i3];
                    if (tritem8.netID != 0)
                    {
                        var netID = tritem8.netID;
                        var stack = tritem8.stack;
                        var prefix = tritem8.prefix;
                        背包.Add(new()
                        {
                            格子 = i3 + 180,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                for (int i4 = 0; i4 < NetItem.VoidSlots; i4++)
                {
                    Item tritem9 = plr.TPlayer.bank4.item[i4];
                    if (tritem9.netID != 0)
                    {
                        var netID = tritem9.netID;
                        var stack = tritem9.stack;
                        var prefix = tritem9.prefix;
                        背包.Add(new()
                        {
                            格子 = i4 + 220,
                            物品 = netID,
                            数量 = stack,
                            前缀 = prefix
                        });
                    }
                }
                // 装备（loadout）144新增的90个格子
                for (int d = 0; d < plr.TPlayer.Loadouts.Length; d++)
                {

                    //tritem10[d].netID;
                    // 装备 和 时装
                    for (int j = 0; j < plr.TPlayer.Loadouts[d].Armor.Length; j++)
                    {
                        Item tritem10 = plr.TPlayer.Loadouts[d].Armor[j];
                        if (tritem10.netID != 0)
                        {
                            if (d == 0)
                            {
                                var netID = tritem10.netID;
                                var stack = tritem10.stack;
                                var prefix = tritem10.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 260,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                            else if (d == 1)
                            {

                                var netID = tritem10.netID;
                                var stack = tritem10.stack;
                                var prefix = tritem10.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 290,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                            else if (d == 2)
                            {
                                var netID = tritem10.netID;
                                var stack = tritem10.stack;
                                var prefix = tritem10.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 320,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                        }
                    }
                    //染料
                    for (int j = 0; j < plr.TPlayer.Loadouts[d].Dye.Length; j++)
                    {
                        Item tritem11 = plr.TPlayer.Loadouts[d].Dye[j];
                        if (tritem11.netID != 0)
                        {
                            if (d == 0)
                            {
                                var netID = tritem11.netID;
                                var stack = tritem11.stack;
                                var prefix = tritem11.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 280,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                            else if (d == 1)
                            {
                                var netID = tritem11.netID;
                                var stack = tritem11.stack;
                                var prefix = tritem11.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 310,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                            else if (d == 2)
                            {
                                var netID = tritem11.netID;
                                var stack = tritem11.stack;
                                var prefix = tritem11.prefix;
                                背包.Add(new()
                                {
                                    格子 = j + 340,
                                    物品 = netID,
                                    数量 = stack,
                                    前缀 = prefix
                                });
                            }
                        }
                    }
                }
                var statLifeMax = plr.TPlayer.statLifeMax;
                var statManaMax = plr.TPlayer.statManaMax;
                var anglerQuestsFinished = plr.TPlayer.anglerQuestsFinished;
                var extraAccessory = plr.TPlayer.extraAccessory;
                var aUsingBiomeTorches = plr.TPlayer.UsingBiomeTorches;
                var ateArtisanBread = plr.TPlayer.ateArtisanBread;
                var usedAegisCrystal = plr.TPlayer.usedAegisCrystal;
                var usedAegisFruit = plr.TPlayer.usedAegisFruit;
                var usedArcaneCrystal = plr.TPlayer.usedArcaneCrystal;
                var usedGalaxyPearl = plr.TPlayer.usedGalaxyPearl;
                var usedGummyWorm = plr.TPlayer.usedGummyWorm;
                var usedAmbrosia = plr.TPlayer.usedAmbrosia;
                var unlockedSuperCart = plr.TPlayer.unlockedSuperCart;
                增益.Add(new()
                {
                    背包 = 背包,
                    血量 = statLifeMax,
                    蓝量 = statManaMax,
                    渔夫任务 = anglerQuestsFinished,
                    恶魔之心 = extraAccessory,
                    火把神 = aUsingBiomeTorches,
                    工匠面包 = ateArtisanBread,
                    生命水晶 = usedAegisCrystal,
                    埃癸斯果 = usedAegisFruit,
                    奥术水晶 = usedArcaneCrystal,
                    银河珍珠 = usedGalaxyPearl,
                    黏性蠕虫 = usedGummyWorm,
                    珍馐 = usedAmbrosia,
                    矿车升级包 = unlockedSuperCart
                });
                return 增益;
            }
            catch
            {
                plr.SendErrorMessage($"[存背包]保存失败！");
                return new() { };
            }
        }

        public static void WritePack(TSPlayer plr, 存背包配置表.数据 p)//修改玩家背包
        {
            for (int i = 0; i < NetItem.MaxInventory; i++)//删除玩家当前背包
            {
                if (i < 59)
                {
                    plr.TPlayer.inventory[i].netDefaults(0);
                }
                else if (i < 79)
                {
                    int index = i - NetItem.ArmorIndex.Item1;
                    plr.TPlayer.armor[index].netDefaults(0);
                }
                else if (i < 89)
                {
                    int index2 = i - NetItem.DyeIndex.Item1;
                    plr.TPlayer.dye[index2].netDefaults(0);
                }
                else if (i < 94)
                {
                    int index3 = i - NetItem.MiscEquipIndex.Item1;
                    plr.TPlayer.miscEquips[index3].netDefaults(0);
                }
                else if (i < 99)
                {
                    int index4 = i - NetItem.MiscDyeIndex.Item1;
                    plr.TPlayer.miscDyes[index4].netDefaults(0);
                }
                else if (i < 139)
                {
                    int index5 = i - NetItem.PiggyIndex.Item1;
                    plr.TPlayer.bank.item[index5].netDefaults(0);
                }
                else if (i < 179)
                {
                    int index6 = i - NetItem.SafeIndex.Item1;
                    plr.TPlayer.bank2.item[index6].netDefaults(0);
                }
                else if (i < 220)
                {
                    if (i == 179)
                    {
                        plr.TPlayer.trashItem.netDefaults(0);
                    }
                    else
                    {
                        int index7 = i - NetItem.ForgeIndex.Item1;
                        plr.TPlayer.bank3.item[index7].netDefaults(0);
                    }
                }
                else if (i < 260)
                {
                    int index8 = i - NetItem.VoidIndex.Item1;
                    plr.TPlayer.bank4.item[index8].netDefaults(0);
                }
                else if (i < 280)
                {
                    int index9 = i - NetItem.Loadout1Armor.Item1;
                    plr.TPlayer.Loadouts[0].Armor[index9].netDefaults(0);
                }
                else if (i < 290)
                {
                    int index10 = i - NetItem.Loadout1Dye.Item1;
                    plr.TPlayer.Loadouts[0].Dye[index10].netDefaults(0);
                }
                else if (i < 310)
                {
                    int index11 = i - NetItem.Loadout2Armor.Item1;
                    plr.TPlayer.Loadouts[1].Armor[index11].netDefaults(0);
                }
                else if (i < 320)
                {
                    int index12 = i - NetItem.Loadout2Dye.Item1;
                    plr.TPlayer.Loadouts[1].Dye[index12].netDefaults(0);
                }
                else if (i < 340)
                {
                    int index13 = i - NetItem.Loadout3Armor.Item1;
                    plr.TPlayer.Loadouts[2].Armor[index13].netDefaults(0);
                }
                else if (i < 350)
                {
                    int index14 = i - NetItem.Loadout3Dye.Item1;
                    plr.TPlayer.Loadouts[2].Dye[index14].netDefaults(0);
                }
                else { }
            }
            for (int j = 0; j < p.背包.Count; j++)//往玩家背包放东西
            {
                var item = p.背包[j];
                Item trItem = TShock.Utils.GetItemById(item.物品);
                trItem.stack = item.数量;
                trItem.prefix = (byte)item.前缀;
                if (item.格子 >= 0 && item.格子 <= 58)
                {
                    plr.TPlayer.inventory[item.格子] = trItem;
                }
                else if (item.格子 >= 59 && item.格子 <= 78)
                {
                    plr.TPlayer.armor[item.格子 - 59] = trItem;
                }
                else if (item.格子 >= 79 && item.格子 <= 88)
                {
                    plr.TPlayer.dye[item.格子 - 79] = trItem;
                }
                else if (item.格子 >= 89 && item.格子 <= 93)
                {
                    plr.TPlayer.miscEquips[item.格子 - 89] = trItem;
                }
                else if (item.格子 >= 94 && item.格子 <= 98)
                {
                    plr.TPlayer.miscDyes[item.格子 - 94] = trItem;
                }
                else if (item.格子 >= 99 && item.格子 <= 138)
                {
                    plr.TPlayer.bank.item[item.格子 - 99] = trItem;
                }
                else if (item.格子 >= 139 && item.格子 <= 178)
                {
                    plr.TPlayer.bank2.item[item.格子 - 139] = trItem;
                }
                else if (item.格子 == 179)
                {
                    plr.TPlayer.trashItem = trItem;
                }
                else if (item.格子 >= 180 && item.格子 <= 219)
                {
                    plr.TPlayer.bank3.item[item.格子 - 180] = trItem;
                }
                else if (item.格子 >= 220 && item.格子 <= 259)
                {
                    plr.TPlayer.bank4.item[item.格子 - 220] = trItem;
                }
                else if (item.格子 >= 260 && item.格子 <= 279)
                {
                    plr.TPlayer.Loadouts[0].Armor[item.格子 - 260] = trItem;
                }
                else if (item.格子 >= 280 && item.格子 <= 289)
                {
                    plr.TPlayer.Loadouts[0].Dye[item.格子 - 280] = trItem;
                }
                else if (item.格子 >= 290 && item.格子 <= 309)
                {
                    plr.TPlayer.Loadouts[1].Armor[item.格子 - 290] = trItem;
                }
                else if (item.格子 >= 310 && item.格子 <= 319)
                {
                    plr.TPlayer.Loadouts[1].Dye[item.格子 - 310] = trItem;
                }
                else if (item.格子 >= 320 && item.格子 <= 339)
                {
                    plr.TPlayer.Loadouts[2].Armor[item.格子 - 320] = trItem;
                }
                else if (item.格子 >= 340 && item.格子 <= 349)
                {
                    plr.TPlayer.Loadouts[2].Dye[item.格子 - 340] = trItem;
                }
            }
            plr.TPlayer.statLifeMax = p.血量;
            plr.TPlayer.statManaMax = p.蓝量;
            plr.TPlayer.anglerQuestsFinished = p.渔夫任务;
            plr.TPlayer.extraAccessory = p.恶魔之心;
            plr.TPlayer.UsingBiomeTorches = p.火把神;
            plr.TPlayer.ateArtisanBread = p.工匠面包;
            plr.TPlayer.usedAegisCrystal = p.生命水晶;
            plr.TPlayer.usedAegisFruit = p.埃癸斯果;
            plr.TPlayer.usedArcaneCrystal = p.奥术水晶;
            plr.TPlayer.usedGalaxyPearl = p.银河珍珠;
            plr.TPlayer.usedGummyWorm = p.黏性蠕虫;
            plr.TPlayer.usedAmbrosia = p.珍馐;
            plr.TPlayer.unlockedSuperCart = p.矿车升级包;
            float slot = 0f;
            for (int k = 0; k < NetItem.InventorySlots; k++)//发送数据包刷新玩家背包
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].inventory[k].Name), plr.Index, slot, (float)Main.player[plr.Index].inventory[k].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int l = 0; l < NetItem.ArmorSlots; l++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].armor[l].Name), plr.Index, slot, (float)Main.player[plr.Index].armor[l].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int m = 0; m < NetItem.DyeSlots; m++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].dye[m].Name), plr.Index, slot, (float)Main.player[plr.Index].dye[m].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int n = 0; n < NetItem.MiscEquipSlots; n++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].miscEquips[n].Name), plr.Index, slot, (float)Main.player[plr.Index].miscEquips[n].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k2 = 0; k2 < NetItem.MiscDyeSlots; k2++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].miscDyes[k2].Name), plr.Index, slot, (float)Main.player[plr.Index].miscDyes[k2].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k3 = 0; k3 < NetItem.PiggySlots; k3++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank.item[k3].Name), plr.Index, slot, (float)Main.player[plr.Index].bank.item[k3].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k4 = 0; k4 < NetItem.SafeSlots; k4++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank2.item[k4].Name), plr.Index, slot, (float)Main.player[plr.Index].bank2.item[k4].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }

            for (int i = 0; i < NetItem.InventorySlots; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.inventory[i].Name, plr.Index, i, plr.TPlayer.inventory[i].prefix);
                //NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.inventory[i].Name),player.Index,i, player.TPlayer.inventory[i].prefix);
            }
            for (int i = 0; i < NetItem.ArmorSlots; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.armor[i].Name, plr.Index, i + 59, plr.TPlayer.armor[i].prefix);
                //NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.armor[i].Name), player.Index, i+59, player.TPlayer.armor[i].prefix);
            }
            for (int i = 0; i < NetItem.DyeSlots; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.dye[i].Name, plr.Index, i + 79, plr.TPlayer.dye[i].prefix);
                //NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.dye[i].Name), player.Index, i+79, player.TPlayer.dye[i].prefix);
            }
            for (int i = 0; i < NetItem.MiscDyeSlots; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.miscDyes[i].Name, plr.Index, i + 94, plr.TPlayer.miscDyes[i].prefix);
                //NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.miscDyes[i].Name), player.Index, i+94, player.TPlayer.miscDyes[i].prefix);
            }
            for (int i = 0; i < NetItem.MiscEquipSlots; i++)
            {
                plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.miscEquips[i].Name, plr.Index, i + 89, plr.TPlayer.miscEquips[i].prefix);
                //NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(player.TPlayer.miscEquips[i].Name), player.Index, i+89, player.TPlayer.miscEquips[i].prefix);
            }
            plr.SendData(PacketTypes.PlayerSlot, plr.TPlayer.trashItem.Name, plr.Index, 179, plr.TPlayer.trashItem.prefix);
            int msgType = 5;
            int remoteClient = -1;
            int ignoreClient = -1;
            NetworkText text = NetworkText.FromLiteral(Main.player[plr.Index].trashItem.Name);
            int index15 = plr.Index;
            float num = slot;
            slot = num + 1f;
            NetMessage.SendData(msgType, remoteClient, ignoreClient, text, index15, num, (float)Main.player[plr.Index].trashItem.prefix, 0f, 0, 0, 0);
            for (int k5 = 0; k5 < NetItem.ForgeSlots; k5++)
            {
                NetMessage.SendData(5, -1, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank3.item[k5].Name), plr.Index, slot, (float)Main.player[plr.Index].bank3.item[k5].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            NetMessage.SendData(4, -1, -1, NetworkText.FromLiteral(plr.Name), plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(42, -1, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(16, -1, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            slot = 0f;
            for (int k6 = 0; k6 < NetItem.InventorySlots; k6++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].inventory[k6].Name), plr.Index, slot, (float)Main.player[plr.Index].inventory[k6].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k7 = 0; k7 < NetItem.ArmorSlots; k7++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].armor[k7].Name), plr.Index, slot, (float)Main.player[plr.Index].armor[k7].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k8 = 0; k8 < NetItem.DyeSlots; k8++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].dye[k8].Name), plr.Index, slot, (float)Main.player[plr.Index].dye[k8].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k9 = 0; k9 < NetItem.MiscEquipSlots; k9++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].miscEquips[k9].Name), plr.Index, slot, (float)Main.player[plr.Index].miscEquips[k9].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k10 = 0; k10 < NetItem.MiscDyeSlots; k10++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].miscDyes[k10].Name), plr.Index, slot, (float)Main.player[plr.Index].miscDyes[k10].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k11 = 0; k11 < NetItem.PiggySlots; k11++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank.item[k11].Name), plr.Index, slot, (float)Main.player[plr.Index].bank.item[k11].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k12 = 0; k12 < NetItem.SafeSlots; k12++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank2.item[k12].Name), plr.Index, slot, (float)Main.player[plr.Index].bank2.item[k12].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k13 = 0; k13 < NetItem.ForgeSlots; k13++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank3.item[k13].Name), plr.Index, slot, (float)Main.player[plr.Index].bank3.item[k13].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k14 = 0; k14 < NetItem.VoidSlots; k14++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank4.item[k14].Name), plr.Index, slot, (float)Main.player[plr.Index].bank4.item[k14].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k15 = 0; k15 < NetItem.LoadoutArmorSlots; k15++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[0].Armor[k15].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[0].Armor[k15].prefix, 0f, 0, 0, 0);
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[1].Armor[k15].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[1].Armor[k15].prefix, 0f, 0, 0, 0);
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[2].Armor[k15].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[2].Armor[k15].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            for (int k16 = 0; k16 < NetItem.LoadoutDyeSlots; k16++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[0].Dye[k16].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[0].Dye[k16].prefix, 0f, 0, 0, 0);
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[1].Dye[k16].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[1].Dye[k16].prefix, 0f, 0, 0, 0);
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].Loadouts[2].Dye[k16].Name), plr.Index, slot, (float)Main.player[plr.Index].Loadouts[2].Dye[k16].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            int msgType2 = 5;
            int index16 = plr.Index;
            int ignoreClient2 = -1;
            NetworkText text2 = NetworkText.FromLiteral(Main.player[plr.Index].trashItem.Name);
            int index17 = plr.Index;
            float num2 = slot;
            slot = num2 + 1f;
            NetMessage.SendData(msgType2, index16, ignoreClient2, text2, index17, num2, (float)Main.player[plr.Index].trashItem.prefix, 0f, 0, 0, 0);
            for (int k13 = 0; k13 < NetItem.ForgeSlots; k13++)
            {
                NetMessage.SendData(5, plr.Index, -1, NetworkText.FromLiteral(Main.player[plr.Index].bank3.item[k13].Name), plr.Index, slot, (float)Main.player[plr.Index].bank3.item[k13].prefix, 0f, 0, 0, 0);
                slot += 1f;
            }
            NetMessage.SendData(4, plr.Index, -1, NetworkText.FromLiteral(plr.Name), plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(42, plr.Index, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(16, plr.Index, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            for (int k14 = 0; k14 < 22; k14++)
            {
                plr.TPlayer.buffType[k14] = 0;
            }
            NetMessage.SendData(50, -1, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(50, plr.Index, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(76, plr.Index, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(76, -1, -1, NetworkText.Empty, plr.Index, 0f, 0f, 0f, 0, 0, 0);
            NetMessage.SendData(39, plr.Index, -1, NetworkText.Empty, 400, 0f, 0f, 0f, 0, 0, 0);
        }


        private void 重载(CommandArgs args)
        {
            try
            {
                Reload();
                args.Player.SendErrorMessage($"[存背包]重载成功！");
            }
            catch
            {
                TSPlayer.Server.SendErrorMessage($"[存背包]配置文件读取错误");
            }
        }
        public static void Reload()
        {
            try
            {
                配置 = JsonConvert.DeserializeObject<存背包配置表>(File.ReadAllText(Path.Combine(TShock.SavePath, "存背包配置表.json")));
                if (配置 != null)
                {
                    int i = 0;
                    foreach (var z in 配置.保存的背包)
                    {
                        z.背包ID = i;
                        i++;
                    }
                }
                File.WriteAllText(path, JsonConvert.SerializeObject(配置, Formatting.Indented));
            }
            catch
            {
                TSPlayer.Server.SendErrorMessage($"[存背包]配置文件读取错误");
            }
        }
    }
}