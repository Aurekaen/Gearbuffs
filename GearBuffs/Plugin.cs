using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace GearBuffs
{
    [ApiVersion(1, 23)]
    public class Plugin : TerrariaPlugin
    {
        public override string Name { get { return "GearBuffs"; } }
        public override string Author { get { return "Alec"; } }
        public override string Description { get { return "Buffs players based on equipment in their inventory"; } }
        public override Version Version { get { return new Version(1, 0, 0, 0); } }

        private static Database db;
        private static Timer updateTimer;
        private static List<GearBuff> _gearBuffs = new List<GearBuff>();
        public Plugin(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            db = Database.InitDb("GearBuffs");
            db.LoadGearBuffs(ref _gearBuffs);

            Commands.ChatCommands.Add(new Command("gearbuffs.add", AddGearBuff, "gearbuff", "gb"));
            ServerApi.Hooks.GamePostInitialize.Register(this, OnPostInitialize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GamePostInitialize.Deregister(this, OnPostInitialize);
            }
            base.Dispose(disposing);
        }

        private void OnPostInitialize(EventArgs args)
        {
            updateTimer = new Timer(1000);
            updateTimer.Elapsed += UpdateTimerOnElapsed;
            updateTimer.Start();
        }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs args)
        {
            foreach (var player in TShock.Players)
            {
                foreach (GearBuff _gb in _gearBuffs)
                {
                    Item _item = TShock.Utils.GetItemById(_gb.item);
                    if (_gb.held == "true")
                    {
                        if (holdingItem(player, _item))
                        {
                            aurabuff(player, _gb);
                            antiaura(player, _gb);
                            giveBuff(player, _gb);
                        }
                    }
                    else if (HasItem(player, _item))
                    {
                        aurabuff(player, _gb);
                        antiaura(player, _gb);
                        giveBuff(player, _gb);
                    }
                    _item = null;
                }
            }
        }

        private void giveBuff(TSPlayer player, GearBuff _gb)
        {
            if(_gb.aura != "antiaura")
            {
                player.SetBuff(_gb.buff, _gb.duration * 60);
            }
        }

        private void aurabuff(TSPlayer player, GearBuff _gb)
        {
            if (_gb.aura == "aura")
            {
                foreach (var ply in TShock.Players)
                {
                    if (ply.Team == player.Team)
                    {
                        if (_gb.range * 15 >= distanceCheck(ply, player))
                        {
                            ply.SetBuff(_gb.buff, _gb.duration * 60);
                        }
                    }
                }
            }
        }

        private void antiaura(TSPlayer player, GearBuff _gb)
        {
            if (_gb.aura == "antiaura")
            {
                foreach (var ply in TShock.Players)
                {
                    if (ply.Team != player.Team)
                    {
                        if (_gb.range * 15 >= distanceCheck(ply, player))
                        {
                            ply.SetBuff(_gb.buff, _gb.duration * 60);
                        }
                    }
                }
            }
        }

        private bool holdingItem(TSPlayer player, Item _item)
        {
            if (player.TPlayer.inventory[player.TPlayer.selectedItem].name == _item.name)
                return true;
            else
                return false;
        }

        private bool HasItem(TSPlayer player, Item _item)
        {
            for (int i = 0; i < 59; i++)
            {
                if (player.TPlayer.inventory[i].name == _item.name)
                    return true;
            }
            for (int i = 0; i < 20; i++)
            {
                if (player.TPlayer.armor[i].name == _item.name)
                    return true;
            }
            return false;
        }

        private int distanceCheck(TSPlayer ply1, TSPlayer ply2)
        {
            float num = ply1.TPlayer.position.X - ply2.TPlayer.position.X;
            float num2 = ply1.TPlayer.position.Y - ply2.TPlayer.position.Y;
            float num3 = num * num + num2 * num2;
            return (int)Math.Sqrt(num3);
        }

        private void AddGearBuff(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                args.Player.SendErrorMessage("Usage:");
                args.Player.SendErrorMessage("/gearbuff add [item] [buff] [duration] [held]");
                args.Player.SendErrorMessage("/gearbuff del [item]");
                args.Player.SendErrorMessage("/gearbuff aura [item] [buff] [duration] [range] [held]");
                args.Player.SendErrorMessage("/gearbuff antiaura [item] [buff] [duration] [range] [held]");
                args.Player.SendErrorMessage("Aliases: /gearbuff, /gb");
                return;
            }
            string GBuffAction = args.Parameters[0].ToString().ToLower();
            if (GBuffAction == "add")
            {
                if (args.Parameters.Count < 3)
                {
                    args.Player.SendErrorMessage("Usage: /gearbuff add [item] [buff] [duration] [held]");
                    return;
                }
                if (args.Parameters.Count >= 3)
                {
                    var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (item.Count == 0)
                    {
                        args.Player.SendErrorMessage("No Item Found.");
                        return;
                    }
                    else if (item.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, item.Select(i => i.name));
                        return;
                    }
                    var itemType = item[0];
                    int itemID = itemType.netID;
                    int buff;
                    int duration;
                    string held = "false";
                    if (!int.TryParse(args.Parameters[2], out buff))
                    {
                        var buffs = TShock.Utils.GetBuffByName(args.Parameters[2]);
                        if (buffs.Count == 0)
                        {
                            args.Player.SendErrorMessage("No Buff Found.");
                            return;
                        }
                        else if (buffs.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, buffs.Select(f => Main.buffName[f]));
                            return;
                        }
                        buff = buffs[0];
                    }
                    if (args.Parameters.Count == 3)
                    {
                        duration = 5;
                    }
                    else if (!int.TryParse(args.Parameters[3], out duration))
                    {
                        args.Player.SendErrorMessage("Invalid duration");
                        return;
                    }
                    if (args.Parameters.Count == 5)
                    {
                        if (args.Parameters[4] == "true")
                        {
                            held = "true";
                        }
                    }
                    var gearbuff = new GearBuff(itemID, buff, duration, held, "none", 0);
                    db.AddGearBuff(gearbuff);
                    Item temp = item[0];
                    args.Player.SendErrorMessage("GearBuff {0} added to {1}", TShock.Utils.GetBuffName(buff), temp.name);
                    _gearBuffs.Add(gearbuff);
                    return;
                }
            }
            if (GBuffAction == "del")
            {
                if (args.Parameters.Count == 1)
                {
                    args.Player.SendErrorMessage("Usage: /gearbuff del [item]");
                    return;
                }
                var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                if (item.Count == 0)
                {
                    args.Player.SendErrorMessage("No Item Found.");
                    return;
                }
                else if (item.Count > 1)
                {
                    TShock.Utils.SendMultipleMatchError(args.Player, item.Select(i => i.name));
                    return;
                }
                var itemType = item[0];
                int itemID = itemType.netID;
                db.DeleteGearBuff(itemID);
                Item temp = TShock.Utils.GetItemById(itemID);
                args.Player.SendErrorMessage("Gearbuffs removed from {0}", temp.name);
                foreach(GearBuff _gearb in _gearBuffs)
                {
                    if (TShock.Utils.GetItemById(_gearb.item).name == temp.name)
                    {

                        _gearBuffs.Remove(_gearBuffs.Find(gbuff => gbuff == _gearb));
                    }
                }
                return;
            }
            if (GBuffAction == "list")
            {
                if (args.Parameters.Count == 2)
                {
                    var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (item.Count == 0)
                    {
                        args.Player.SendErrorMessage("Item Not Found.");
                        return;
                    }
                    else if (item.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, item.Select(i => i.name));
                        return;
                    }
                    var itemType = item[0];
                    int itemID = itemType.netID;
                    int pagenum;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, 2, args.Player, out pagenum))
                        return;
                    IEnumerable<string> gearBuffNames = from GearBuff in _gearBuffs
                                                        where GearBuff.item == itemID
                                                        select TShock.Utils.GetBuffName(GearBuff.buff);
                    PaginationTools.SendPage(args.Player, pagenum, PaginationTools.BuildLinesFromTerms(gearBuffNames),
                        new PaginationTools.Settings
                        {
                            HeaderFormat = "Gearbuffs for " + itemType.name,
                            FooterFormat = "Type /gb list {{0}}",
                            NothingToDisplayString = "There are no gearbuffs for " + itemType.name
                        });
                    return;
                }
                else
                {
                    args.Player.SendErrorMessage("Usage: /gearbuff list [item]");
                    return;
                }
            }
            if (GBuffAction == "aura")
            {
                if (args.Parameters.Count < 5)
                {
                    args.Player.SendErrorMessage("Usage: /gearbuff aura [item] [buff] [duration] [range] [held]");
                    return;
                }
                if (args.Parameters.Count >= 5)
                {
                    var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (item.Count == 0)
                    {
                        args.Player.SendErrorMessage("No Item Found.");
                        return;
                    }
                    else if (item.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, item.Select(i => i.name));
                        return;
                    }
                    var itemType = item[0];
                    int itemID = itemType.netID;
                    int buff;
                    int duration;
                    string held = "";
                    if (!int.TryParse(args.Parameters[2], out buff))
                    {
                        var buffs = TShock.Utils.GetBuffByName(args.Parameters[2]);
                        if (buffs.Count == 0)
                        {
                            args.Player.SendErrorMessage("No Buff Found.");
                            return;
                        }
                        else if (buffs.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, buffs.Select(f => Main.buffName[f]));
                            return;
                        }
                        buff = buffs[0];
                    }
                    if (!int.TryParse(args.Parameters[3], out duration))
                    {
                        args.Player.SendErrorMessage("Invalid duration");
                        return;
                    }
                    int range;
                    if (!int.TryParse(args.Parameters[4], out range))
                    {
                        args.Player.SendErrorMessage("Invalid Range");
                        return;
                    }

                    if (args.Parameters.Count == 6)
                    {
                        if (args.Parameters[5] == "true")
                        {
                            held = "true";
                        }
                    }
                    if (held != "true")
                    {
                        held = "false";
                    }
                    string aura = "aura";
                    var gearbuff = new GearBuff(itemID, buff, duration, held, aura, range);
                    db.AddGearBuff(gearbuff);
                    Item temp = item[0];
                    args.Player.SendErrorMessage("GearBuff {0} added to {1} with a friendly range of {2}", TShock.Utils.GetBuffName(buff), temp.name, range);
                    _gearBuffs.Add(gearbuff);
                    return;
                }
            }
            if (GBuffAction == "antiaura")
            {
                if (args.Parameters.Count < 4)
                {
                    args.Player.SendErrorMessage("Usage: /gearbuff antiaura [item] [buff] [duration] [range] [held]");
                    return;
                }
                if (args.Parameters.Count >= 4)
                {
                    var item = TShock.Utils.GetItemByIdOrName(args.Parameters[1]);
                    if (item.Count == 0)
                    {
                        args.Player.SendErrorMessage("No Item Found.");
                        return;
                    }
                    else if (item.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, item.Select(i => i.name));
                        return;
                    }
                    var itemType = item[0];
                    int itemID = itemType.netID;
                    int buff;
                    int duration;
                    string held = "false";
                    if (!int.TryParse(args.Parameters[2], out buff))
                    {
                        var buffs = TShock.Utils.GetBuffByName(args.Parameters[2]);
                        if (buffs.Count == 0)
                        {
                            args.Player.SendErrorMessage("No Buff Found.");
                            return;
                        }
                        else if (buffs.Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, buffs.Select(f => Main.buffName[f]));
                            return;
                        }
                        buff = buffs[0];
                    }
                    if (!int.TryParse(args.Parameters[3], out duration))
                    {
                        args.Player.SendErrorMessage("Invalid duration");
                        return;
                    }
                    int range;
                    if (!int.TryParse(args.Parameters[4], out range))
                    {
                        args.Player.SendErrorMessage("Invalid Range");
                        return;
                    }

                    if (args.Parameters.Count == 6)
                    {
                        if (args.Parameters[5] == "true")
                        {
                            held = "true";
                        }
                    }
                    if (held != "true")
                    {
                        held = "false";
                    }
                    string aura = "antiaura";
                    var gearbuff = new GearBuff(itemID, buff, duration, held, aura, range);
                    db.AddGearBuff(gearbuff);
                    Item temp = item[0];
                    args.Player.SendErrorMessage("GearBuff {0} added to {1} with a hostile range of {2}", TShock.Utils.GetBuffName(buff), temp.name, range);
                    _gearBuffs.Add(gearbuff);
                    return;
                }
            }
            if (GBuffAction == "refresh")
            {
                args.Player.SendErrorMessage("Gearbuffs Refreshed!");
                _gearBuffs.Clear();
                db.LoadGearBuffs(ref _gearBuffs);
                return;
            }
            if (GBuffAction == "disable")
            {
                if (updateTimer.Enabled)
                {
                    updateTimer.Enabled = false;
                    args.Player.SendErrorMessage("Gearbuffs Disabled");
                    return;
                }
                else
                {
                    args.Player.SendErrorMessage("Gearbuffs Already Disabled");
                    return;
                }
            }
            if (GBuffAction == "enable")
            {
                if (updateTimer.Enabled)
                {
                    args.Player.SendErrorMessage("Gearbuffs Already Enabled");
                    return;
                }
                else
                {
                    updateTimer.Enabled = true;
                    args.Player.SendErrorMessage("Gearbuffs Enabled");
                    return;
                }
            }
            args.Player.SendErrorMessage("Usage:");
            args.Player.SendErrorMessage("/gearbuff add [item] [buff] [duration] [held]");
            args.Player.SendErrorMessage("/gearbuff del [item]");
            args.Player.SendErrorMessage("/gearbuff aura [item] [buff] [duration] [range] [held]");
            args.Player.SendErrorMessage("/gearbuff antiaura [item] [buff] [duration] [range] [held]");
            return;
        }
    }
}
