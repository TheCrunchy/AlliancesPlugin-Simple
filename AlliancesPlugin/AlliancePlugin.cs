using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using Torch.API.Managers;
using System.IO;
using VRage.Game.ModAPI;
using NLog;
using VRageMath;
using Sandbox.ModAPI;
using Sandbox.Game.Entities.Character;
using VRage.Game;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using VRage.ObjectBuilders;
using VRage;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using Torch.Mod.Messages;
using Torch.Mod;
using Torch.Managers.ChatManager;
using Torch.Managers;
using Torch.API.Plugins;
using AlliancesPlugin.Alliances;
using VRage.Utils;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;
using Sandbox.Game.Screens.Helpers;
using System.Globalization;
using System.Text.RegularExpressions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Blocks;
using SpaceEngineers.Game.Entities.Blocks.SafeZone;
using DSharpPlus;
using SpaceEngineers.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using HarmonyLib;
using Sandbox.ModAPI.Weapons;
using Sandbox.Game.Weapons;
using Sandbox.Game;
using System.Threading.Tasks;

namespace AlliancesPlugin
{
    public class AlliancePlugin : TorchPluginBase
    {
        public static MethodInfo sendChange;
        public static TorchSessionState TorchState;
        private TorchSessionManager sessionManager;
        public static Config config;

        public static string path;
        public static string basePath;
        public static Logger Log = LogManager.GetLogger("Alliances");
        public DateTime NextUpdate = DateTime.Now;
        public static Dictionary<Guid, List<ulong>> playersInAlliances = new Dictionary<Guid, List<ulong>>();
        public static Dictionary<ulong, Guid> playersAllianceId = new Dictionary<ulong, Guid>();

        private static List<DateTime> captureIntervals = new List<DateTime>();
        private static Dictionary<String, int> amountCaptured = new Dictionary<String, int>();


        public DateTime NextMining = DateTime.Now;

        public static FileUtils utils = new FileUtils();
        public static ITorchPlugin GridBackup;
        public static MethodInfo BackupGrid;

        public static ITorchBase TorchBase;
        public static bool GridBackupInstalled = false;
        public static Dictionary<MyDefinitionId, int> ItemUpkeep = new Dictionary<MyDefinitionId, int>();
        public static void InitPluginDependencies(PluginManager Plugins)
        {

            if (Plugins.Plugins.TryGetValue(Guid.Parse("75e99032-f0eb-4c0d-8710-999808ed970c"), out ITorchPlugin GridBackupPlugin))
            {

                BackupGrid = GridBackupPlugin.GetType().GetMethod("BackupGridsManuallyWithBuilders", BindingFlags.Public | BindingFlags.Instance, null, new Type[2] { typeof(List<MyObjectBuilder_CubeGrid>), typeof(long) }, null);
                GridBackup = GridBackupPlugin;
                GridBackupInstalled = true;
            }

        }

        public static void BackupGridMethod(List<MyObjectBuilder_CubeGrid> Grids, long User)
        {
            try
            {
                BackupGrid?.Invoke(GridBackup, new object[] { Grids, User });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }


    

        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.AddOverrideMod(758597413L);
            sessionManager.AddOverrideMod(2612907530L);
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            basePath = StoragePath;
            SetupConfig();
            path = CreatePath();




            if (!Directory.Exists(path + "//PlayerData//"))
            {
                Directory.CreateDirectory(path + "//PlayerData//");
            }
            TorchBase = Torch;
            LoadAllAlliances();




        }

        public void SetupConfig()
        {
            FileUtils utils = new FileUtils();
            path = StoragePath;
            if (File.Exists(StoragePath + "\\Alliances.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\Alliances.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\Alliances.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\Alliances.xml", config, false);
            }

        }
        public string CreatePath()
        {

            var folder = "";
            if (config.StoragePath.Equals("default"))
            {
                folder = Path.Combine(StoragePath + "//Alliances");
            }
            else
            {
                folder = config.StoragePath;
            }
            var folder2 = "";
            Directory.CreateDirectory(folder);
            folder2 = Path.Combine(StoragePath + "//Alliances//KOTH//");
            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//Alliances//AllianceData");
            }
            else
            {
                folder2 = config.StoragePath + "//AllianceData";
            }

            Directory.CreateDirectory(folder2);
            if (config.StoragePath.Equals("default"))
            {
                folder2 = Path.Combine(StoragePath + "//Alliances//ShipyardData");
            }
            else
            {
                folder2 = config.StoragePath + "//Alliance//ShipyardData";
            }

            Directory.CreateDirectory(folder);
            return folder;
        }

        public static Config LoadConfig()
        {
            FileUtils utils = new FileUtils();

            config = utils.ReadFromXmlFile<Config>(basePath + "\\Alliances.xml");
          

            return config;
        }

     
        public static void saveConfig()
        {
            FileUtils utils = new FileUtils();

            utils.WriteToXmlFile<Config>(path + "\\Alliances.xml", config);

            return;
        }
        public static void SaveAllianceData(Alliance alliance)
        {
            FileUtils jsonStuff = new FileUtils();

            jsonStuff.WriteToJsonFile<Alliance>(path + "//AllianceData//" + alliance.AllianceId + ".json", alliance);
            AlliancePlugin.AllAlliances[alliance.name] = alliance;
        }
        public static Alliance LoadAllianceData(Guid id)
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                Alliance alliance2 = jsonStuff.ReadFromJsonFile<Alliance>(path + "//AllianceData//" + id + ".json");
                return alliance2;
            }
            catch
            {
                return null;
            }
        }
        public static Alliance GetAlliance(string name)
        {
            //fuck it lets just return something that might be null
            Alliance temp = null;
            if (AllAlliances.ContainsKey(name))
            {
                temp = LoadAllianceData(AllAlliances[name].AllianceId);
            }

            //i null check in the command anyway

            return temp;
        }
        public static Alliance GetAllianceNoLoading(string name)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.name.Equals(name))
                {

                    return alliance.Value;
                }
            }
            return null;
        }
        public static Alliance GetAlliance(Guid guid)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceId == guid)
                {

                    return GetAlliance(alliance.Value.name);
                }
            }
            return null;
        }
        public static Alliance GetAllianceNoLoading(Guid guid)
        {
            //fuck it lets just return something that might be null
            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceId == guid)
                {

                    return alliance.Value;
                }
            }
            return null;
        }
        public static Dictionary<long, String> FactionsInAlliances = new Dictionary<long, string>();
        public static Alliance GetAllianceNoLoading(MyFaction fac)
        {
            //fuck it lets just return something that might be null
            if (FactionsInAlliances.ContainsKey(fac.FactionId))
            {
                return AllAlliances[FactionsInAlliances[fac.FactionId]];
            }
            return null;
        }
        public static Alliance GetAlliance(MyFaction fac)
        {
            //fuck it lets just return something that might be null
            if (FactionsInAlliances.ContainsKey(fac.FactionId))
            {
                return GetAlliance(FactionsInAlliances[fac.FactionId]);
            }

            foreach (KeyValuePair<String, Alliance> alliance in AllAlliances)
            {
                if (alliance.Value.AllianceMembers.Contains(fac.FactionId))
                {

                    return GetAlliance(alliance.Value.name);
                }
            }
            return null;
        }
        public void SetupFriendMethod()
        {
            Type FactionCollection = MySession.Static.Factions.GetType().Assembly.GetType("Sandbox.Game.Multiplayer.MyFactionCollection");
            sendChange = FactionCollection?.GetMethod("SendFactionChange", BindingFlags.NonPublic | BindingFlags.Static);
        }
        private static List<Vector3> StationLocations = new List<Vector3>();
        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }
        public static ChatManagerServer _chatmanager;

        public static long GetAttacker(long attackerId)
        {

            var entity = MyAPIGateway.Entities.GetEntityById(attackerId);

            if (entity == null)
                return 0L;

            if (entity is MyPlanet)
            {

                return 0L;
            }

            if (entity is MyCharacter character)
            {

                return character.GetPlayerIdentityId();
            }

            if (entity is IMyEngineerToolBase toolbase)
            {

                return toolbase.OwnerIdentityId;

            }

            if (entity is MyLargeTurretBase turret)
            {

                return turret.OwnerId;

            }

            if (entity is MyShipToolBase shipTool)
            {

                return shipTool.OwnerId;
            }

            if (entity is IMyGunBaseUser gunUser)
            {

                return gunUser.OwnerId;

            }



            if (entity is MyCubeGrid grid)
            {

                var gridOwnerList = grid.BigOwners;
                var ownerCnt = gridOwnerList.Count;
                var gridOwner = 0L;

                if (ownerCnt > 0 && gridOwnerList[0] != 0)
                    gridOwner = gridOwnerList[0];
                else if (ownerCnt > 1)
                    gridOwner = gridOwnerList[1];

                return gridOwner;

            }

            return 0L;
        }


     


        public static void NEWSUIT(MyEntity entity)
        {
            if (entity is MyCharacter character)
            {
           //     AlliancePlugin.Log.Info("ITS A SUIT BITCH!");

           
            }
        }
        public static Random rand = new Random();
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (state == TorchSessionState.Unloading)
            {

                // DiscordStuff.DisconnectDiscord();
                TorchState = TorchSessionState.Unloading;
            }
            if (state == TorchSessionState.Loaded)
            {
             // MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1, new BeforeDamageApplied(DamageHandler));
             //   MyEntities.OnEntityAdd += NEWSUIT;
                if (config != null && config.AllowDiscord && !DiscordStuff.Ready)
                {
                    DiscordStuff.Ready = true ;
                }
                nextRegister = DateTime.Now;
                //    rand.Next(1, 60);

       

             // MyBankingSystem.Static.OnAccountBalanceChanged += BalanceChangedMethod2;

                AllianceChat.ApplyLogging();
                InitPluginDependencies(Torch.Managers.GetManager<PluginManager>());
                TorchState = TorchSessionState.Loaded;
                _chatmanager = Torch.CurrentSession.Managers.GetManager<ChatManagerServer>();

                if (_chatmanager == null)
                {
                    Log.Warn("No chat manager loaded!");
                }
                else
                {
                    _chatmanager.MessageProcessing += AllianceChat.DoChatMessage;

                    session.Managers.GetManager<IMultiplayerManagerBase>().PlayerJoined += AllianceChat.Login;
                    session.Managers.GetManager<IMultiplayerManagerBase>().PlayerLeft += AllianceChat.Logout;

                }


                // MySession.Static.Config.
                // MyMultiplayer.Static.SyncDistance

                //      if (!File.Exists(path + "//bank.db"))
                //    {
                //     File.Create(path + "//bank.db");
                // }
                if (!Directory.Exists(basePath + "//Alliances"))
                {
                    Directory.CreateDirectory(basePath + "//Alliances//");
                }


                SetupFriendMethod();

                LoadAllAlliances();





                foreach (Alliance alliance in AllAlliances.Values)
                {
                    try
                    {
                        alliance.ForceFriendlies();
                        alliance.ForceEnemies();
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                    if (alliance.DiscordChannelId > 0 && !String.IsNullOrEmpty(alliance.DiscordToken) && TorchState == TorchSessionState.Loaded)
                    {
                        //  Log.Info(Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length);

                        try
                        {
                            if (Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length != 59)
                            {
                                Log.Error("Invalid bot token for " + alliance.AllianceId);
                                continue;
                            }

                        }
                        catch (Exception ex)
                        {
                            //  Log.Error(ex);
                            Log.Error("Invalid bot token for " + alliance.AllianceId);
                            continue;
                        }
                        //    if (!botsTried.Contains(alliance.AllianceId))
                        //    {
                        //   botsTried.Add(alliance.AllianceId);
                        Log.Info("Registering bot for " + alliance.AllianceId);
                        registerThese.Add(alliance.AllianceId, nextRegister.AddSeconds(15));





                    }
                }
                //        DatabaseForBank bank = new DatabaseForBank();
                //    bank.CreateTable(bank.CreateConnection());
            }
        }
 

        public static DateTime nextRegister = DateTime.Now.AddSeconds(60);
        public static Dictionary<Guid, DateTime> registerThese = new Dictionary<Guid, DateTime>();


      
       
        public int ticks;
        public static MyIdentity TryGetIdentity(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
        public static Dictionary<String, Alliance> AllAlliances = new Dictionary<string, Alliance>();

        public static void LoadAllAlliancesForUpkeep()
        {
            FileUtils jsonStuff = new FileUtils();
            try
            {
                AllAlliances.Clear();
                FactionsInAlliances.Clear();
                foreach (String s in Directory.GetFiles(path + "//AllianceData//"))
                {

                    Alliance alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                    if (AllAlliances.ContainsKey(alliance.name))
                    {
                        alliance.name += " DUPLICATE";
                        AllAlliances.Add(alliance.name, alliance);

                        foreach (long id in alliance.AllianceMembers)
                        {
                            if (!FactionsInAlliances.ContainsKey(id))
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }
                        SaveAllianceData(alliance);
                    }
                    else
                    {
                        AllAlliances.Add(alliance.name, alliance);
                        foreach (long id in alliance.AllianceMembers)
                        {
                            if (!FactionsInAlliances.ContainsKey(id))
                            {
                                FactionsInAlliances.Add(id, alliance.name);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
        public static Boolean HasFailedUpkeep(Alliance alliance)
        {
            if (alliance.failedUpkeep > 0)
            {
                return true;
            }
            return false;
        }

        public void LoadAllAlliances()
        {
            if (TorchState == TorchSessionState.Loaded)
            {
                FileUtils jsonStuff = new FileUtils();
                try
                {
                    AllAlliances.Clear();
                    FactionsInAlliances.Clear();
                    foreach (String s in Directory.GetFiles(path + "//AllianceData//"))
                    {

                        Alliance alliance = jsonStuff.ReadFromJsonFile<Alliance>(s);
                        if (AllAlliances.ContainsKey(alliance.name))
                        {
                            alliance.name += " DUPLICATE";
                            AllAlliances.Add(alliance.name, alliance);

                            foreach (long id in alliance.AllianceMembers)
                            {
                                if (!FactionsInAlliances.ContainsKey(id))
                                {
                                    FactionsInAlliances.Add(id, alliance.name);
                                }
                            }
                            SaveAllianceData(alliance);
                        }
                        else
                        {
                            AllAlliances.Add(alliance.name, alliance);
                            foreach (long id in alliance.AllianceMembers)
                            {
                                if (!FactionsInAlliances.ContainsKey(id))
                                {
                                    FactionsInAlliances.Add(id, alliance.name);
                                }
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                //  Log.Info("Registering bots");
                foreach (Alliance alliance in AllAlliances.Values)
                {
                    alliance.ForceFriendlies();
                    alliance.ForceEnemies();
                    if (DiscordStuff.allianceBots.TryGetValue(alliance.AllianceId, out DiscordClient bot))
                    {
                        //   bot.MessageCreated -= DiscordStuff.Discord_AllianceMessage;
                        //  bot.MessageCreated += DiscordStuff.Discord_AllianceMessage;

                    }
                    if (alliance.DiscordChannelId > 0 && !String.IsNullOrEmpty(alliance.DiscordToken) && TorchState == TorchSessionState.Loaded)
                    {
                        //  Log.Info(Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length);

                        try
                        {
                            if (Encryption.DecryptString(alliance.AllianceId.ToString(), alliance.DiscordToken).Length != 59)
                            {
                                Log.Error("Invalid bot token for " + alliance.AllianceId);

                                continue;
                            }

                        }
                        catch (Exception ex)
                        {
                            //  Log.Error(ex);
                            Log.Error("Invalid bot token for " + alliance.AllianceId);
                            continue;
                        }

                        //    if (!botsTried.Contains(alliance.AllianceId))
                        //    {
                        //   botsTried.Add(alliance.AllianceId);
                        //    DiscordStuff.RegisterAllianceBot(alliance, alliance.DiscordChannelId);




                    }
                }

            }
        }
        public static List<IMyIdentity> GetAllIdentitiesByNameOrId(string playerNameOrSteamId)
        {
            List<IMyIdentity> ids = new List<IMyIdentity>();
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                {
                    if (!ids.Contains(identity))
                    {
                        ids.Add(identity);
                    }
                }
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                    {
                        if (!ids.Contains(identity))
                        {
                            ids.Add(identity);
                        }

                    }
                    if (identity.IdentityId == (long)steamId)
                    {
                        if (!ids.Contains(identity))
                        {
                            ids.Add(identity);
                        }
                    }
                }

            }
            return ids;
        }

     
    
        public static string WorldName = "";
   
       
   
        public void OrganisePlayers()
        {
            foreach (MyPlayer player in MySession.Static.Players.GetOnlinePlayers())
            {
                if (MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId) != null)
                {
                    Alliance temp = GetAllianceNoLoading(MySession.Static.Factions.GetPlayerFaction(player.Identity.IdentityId));
                    if (temp != null)
                    {
                        //if (AllianceChat.PeopleInAllianceChat.ContainsKey(player.Id.SteamId))
                        //{
                        //    AllianceCommands.SendStatusToClient(true, player.Id.SteamId);
                        //}
                        //else
                        //{
                        //    AllianceCommands.SendStatusToClient(false, player.Id.SteamId);
                        //}
                        if (playersInAlliances.ContainsKey(temp.AllianceId))
                        {
                            if (!playersInAlliances[temp.AllianceId].Contains(player.Id.SteamId))
                            {
                                playersInAlliances[temp.AllianceId].Add(player.Id.SteamId);
                            }
                            if (!playersAllianceId.ContainsKey(player.Id.SteamId))
                            {
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                        }
                        else
                        {
                            List<ulong> bob = new List<ulong>();
                            bob.Add(player.Id.SteamId);
                            playersInAlliances.Add(temp.AllianceId, bob);

                            if (!playersAllianceId.ContainsKey(player.Id.SteamId))
                            {
                                playersAllianceId.Add(player.Id.SteamId, temp.AllianceId);
                            }
                        }
                    }
                }
            }
        }
        public static List<long> TaxingId = new List<long>();
        public static List<long> OtherTaxingId = new List<long>();
    
        public static bool yeeted = false;

        public static bool Paused = false;
 

        public static DateTime chat = DateTime.Now;

        public static Dictionary<ulong, DateTime> UpdateThese = new Dictionary<ulong, DateTime>();
        public static DateTime RegisterMainBot = DateTime.Now;
        public override void Update()
        {

            List<ulong> YEET = new List<ulong>();
            foreach (KeyValuePair<ulong, DateTime> pair in UpdateThese)
            {
                if (DateTime.Now >= pair.Value)
                {
                    if (AllianceChat.PeopleInAllianceChat.ContainsKey(pair.Key))
                    {
                        AllianceCommands.SendStatusToClient(true, pair.Key);
                    }
                    else
                    {
                        AllianceCommands.SendStatusToClient(false, pair.Key);
                    }
                }
            }
            foreach (ulong id in YEET)
            {
                UpdateThese.Remove(id);
            }
            ticks++;

            if (ticks % 512 == 0)
            {

                if (config.AllowDiscord)
                {
                    Dictionary<Guid, DateTime> temp = new Dictionary<Guid, DateTime>();
                    foreach (KeyValuePair<Guid, DateTime> keys in registerThese)
                    {
                        if (DateTime.Now > keys.Value)
                        {
                            Alliance alliance = GetAlliance(keys.Key);
                            if (alliance != null)
                            {

                                DiscordStuff.RegisterAllianceBot(alliance, alliance.DiscordChannelId);

                                temp.Add(alliance.AllianceId, DateTime.Now.AddMinutes(10));
                                Log.Info("Connecting bot.");
                            }

                        }
                    }
                    foreach (KeyValuePair<Guid, DateTime> keys in temp)
                    {
                        registerThese[keys.Key] = keys.Value;
                    }
                }
            }
            if (DateTime.Now > chat)
            {

                chat = chat.AddMinutes(10);
            }

            if (DateTime.Now > NextUpdate && TorchState == TorchSessionState.Loaded)
            {
                Log.Info("Doing alliance tasks");

                DateTime now = DateTime.Now;
                //   if (config != null && config.AllowDiscord && !DiscordStuff.Ready && now >= RegisterMainBot)
                //    {
                //         DiscordStuff.RegisterDiscord();
                //    }
                NextUpdate = now.AddSeconds(60);
  

                try
                {
                    LoadAllAlliances();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
             
                try
                {
                    OrganisePlayers();
                }
                catch (Exception ex)
                {
                    Log.Error(ex);

                }
            }

           

        }
  

     
        public static Alliance GetNationTag(IMyFaction fac)
        {
            if (GetAllianceNoLoading(fac as MyFaction) != null)
            {
                return GetAllianceNoLoading(fac as MyFaction);
            }
            return null;
        }
        public static void SendChatMessage(String prefix, String message, ulong steamID = 0)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = prefix;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = Color.OrangeRed;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId(steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
        public static string GetPlayerName(ulong steamId)
        {
            MyIdentity id = GetIdentityByNameOrId(steamId.ToString());
            if (id != null && id.DisplayName != null)
            {
                return id.DisplayName;
            }
            else
            {
                return steamId.ToString();
            }
        }
        public static MyIdentity GetIdentityByNameOrId(string playerNameOrSteamId)
        {
            foreach (var identity in MySession.Static.Players.GetAllIdentities())
            {
                if (identity.DisplayName == playerNameOrSteamId)
                    return identity;
                if (ulong.TryParse(playerNameOrSteamId, out ulong steamId))
                {
                    ulong id = MySession.Static.Players.TryGetSteamId(identity.IdentityId);
                    if (id == steamId)
                        return identity;
                    if (identity.IdentityId == (long)steamId)
                        return identity;
                }

            }
            return null;
        }
    }
}
