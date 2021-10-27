﻿using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;
using VRage.Game;
using System.Numerics;
using VRageMath;

namespace AlliancesPlugin.Alliances
{
    public class Alliance
    {
        public Guid AllianceId = System.Guid.NewGuid();
        public String name;
        public String description;
        public ulong SupremeLeader;
        public string LeaderTitle = "Supreme Leader";
        public Boolean AllowElections = false;
        public List<long> BlockedFactions = new List<long>();
        public Dictionary<ulong, String> otherTitles = new Dictionary<ulong, string>();
        public string DiscordToken = string.Empty;
        public ulong DiscordChannelId = 0;
        public List<String> enemies = new List<String>();
        public List<long> EnemyFactions = new List<long>();
        public int reputation = 0;
        public List<long> Invites = new List<long>();
        public List<long> AllianceMembers = new List<long>();
        public int GridRepairUpgrade = 0;
        public long bankBalance = 0;
        public Boolean hasUnlockedShipyard = false;
        public Boolean hasUnlockedHangar = false;
        FileUtils utils = new FileUtils();

        public Dictionary<String, RankPermissions> CustomRankPermissions = new Dictionary<string, RankPermissions>();
        public Dictionary<ulong, String> PlayersCustomRank = new Dictionary<ulong, string>();
        public RankPermissions UnrankedPerms = new RankPermissions();
        public int CurrentMetaPoints = 0;
        public Dictionary<string, string> inheritance = new Dictionary<string, string>();
        public Dictionary<ulong, RankPermissions> playerPermissions = new Dictionary<ulong, RankPermissions>();

        public bool ElectionCycle = false;
        public long ShipyardFee = 25000000;
        public int r = 66;
        public int g = 163;
        public int b = 237;
        public int failedUpkeep = 0;
        public int RefineryUpgradeLevel = 0;
        public int AssemblerUpgradeLevel = 0;

        public Boolean ForceFriends = true;
     
        public Boolean HasInheritance(String rank)
        {
            if (inheritance.ContainsKey(rank))
            {
                return true;
            }
            return false;
        }

        public List<AccessLevel> GetInheritedPermissions(String rank)
        {
            List<AccessLevel> levels = new List<AccessLevel>();

            if (inheritance.TryGetValue(rank, out string minion))
            {
                levels.AddRange(CustomRankPermissions[rank].permissions);
                if (inheritance.ContainsKey(minion))
                {
                    levels.AddRange(GetInheritedPermissions(minion));
                }
            }

            return levels;
        }
        public Boolean HasInheritedAccess(AccessLevel level, string Rank)
        {
            List<AccessLevel> levels = new List<AccessLevel>();
            levels.AddRange(GetInheritedPermissions(Rank));

            return false;
        }
        public Boolean HasAccess(ulong id, AccessLevel level)
        {
            
            if (SupremeLeader == id)
            {
                return true;
            }

            if (PlayersCustomRank.ContainsKey(id))
            {
                if (CustomRankPermissions[PlayersCustomRank[id]].permissions.Contains(AccessLevel.Everything))
                {
                    return true;
                }
                if (CustomRankPermissions[PlayersCustomRank[id]].permissions.Contains(level))
                {
                    return true;
                }
                else
                {
                    if (HasInheritedAccess(level, PlayersCustomRank[id]))
                    {
                        return true;
                    }
                }
            }
            if (playerPermissions.ContainsKey(id))
            {
                if (playerPermissions[id].permissions.Contains(AccessLevel.Everything))
                {
                    return true;
                }
                if (playerPermissions[id].permissions.Contains(level))
                {
                    return true;
                }
            }

            if (UnrankedPerms.permissions.Contains(level))
            {
                return true;
            }


            return false;
        }

        public int GetFactionCount()
        {
            int count = 0;
            foreach (long id in this.AllianceMembers)
            {
                if (MySession.Static.Factions.TryGetFactionById(id) != null)
                {
                    count++;
                }
            }
            return count;
        }
        public float leadertax = 0;
        public float GetTaxRate(ulong id)
        {


            if (SupremeLeader == id)
            {
                return leadertax;
            }
            if (HasAccess(id, AccessLevel.TaxExempt))
            {
                return 0;
            }


            if (PlayersCustomRank.ContainsKey(id))
            {
                if (CustomRankPermissions.ContainsKey(PlayersCustomRank[id]))
                {
                    return CustomRankPermissions[PlayersCustomRank[id]].taxRate;
                }
            }




            return UnrankedPerms.taxRate;
        }
        public string GetTitle(ulong id)
        {
            if (SupremeLeader == id)
            {
                return LeaderTitle;
            }
            if (otherTitles.ContainsKey(id))
            {
                return otherTitles[id];
            }
            if (PlayersCustomRank.ContainsKey(id))
            {
                return PlayersCustomRank[id];
            }
        


            return "";
        }
        public string OutputMembers()
        {

            StringBuilder sb = new StringBuilder();
            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag);
                    foreach (KeyValuePair<long, MyFactionMember> member in fac.Members)
                    {
                        sb.AppendLine(AlliancePlugin.GetPlayerName(MySession.Static.Players.TryGetSteamId(member.Value.PlayerId)));
                    }
                    sb.AppendLine("");
                }
            }
            return sb.ToString();
        }
    

        private Dictionary<String, StringBuilder> otherTitlesDic = new Dictionary<string, StringBuilder>();



        public string OutputAlliance()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Alliance Leader: " + LeaderTitle + " " + AlliancePlugin.GetPlayerName(SupremeLeader));
            if (!String.IsNullOrEmpty(description))
            {
                sb.AppendLine("");
                sb.AppendLine(description);
            }
            sb.AppendLine("");
            sb.AppendLine("Meta Points : " + String.Format("{0:n0}", CurrentMetaPoints));
            sb.AppendLine("");

            StringBuilder perms = new StringBuilder();
            foreach (KeyValuePair<String, RankPermissions> customs in CustomRankPermissions)
            {
                perms.Clear();
                foreach (AccessLevel level in customs.Value.permissions)
                {
                    perms.Append(level.ToString() + ", ");
                }
                sb.AppendLine("");
                sb.AppendLine(customs.Key + " Permissions : " + perms.ToString());
              //  sb.AppendLine(customs.Key + " tax rate : " + CustomRankPermissions[customs.Key].taxRate * 100 + "%");
            }

            sb.AppendLine("");
            perms.Clear();
            foreach (AccessLevel level in UnrankedPerms.permissions)
            {
                perms.Append(level.ToString() + ", ");
            }
            sb.AppendLine("Unranked Permissions : " + perms.ToString());
          //  sb.AppendLine("Unranked tax rate : " + UnrankedPerms.taxRate * 100 + "%");
            sb.AppendLine("");
            otherTitlesDic.Clear();
            foreach (KeyValuePair<ulong, String> titles in PlayersCustomRank)
            {
                if (otherTitlesDic.ContainsKey(titles.Value))
                {

                    otherTitlesDic[titles.Value].AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                }
                else
                {
                    StringBuilder sbb = new StringBuilder();
                    sbb.AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                    otherTitlesDic.Add(titles.Value, sbb);
                }

            }
            foreach (KeyValuePair<String, StringBuilder> key in otherTitlesDic)
            {
                sb.AppendLine(key.Value.ToString());
            }
            otherTitlesDic.Clear();
            foreach (KeyValuePair<ulong, String> titles in otherTitles)
            {
                if (otherTitlesDic.ContainsKey(titles.Value))
                {

                    otherTitlesDic[titles.Value].AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                }
                else
                {
                    StringBuilder sbb = new StringBuilder();
                    sbb.AppendLine(titles.Value + " " + AlliancePlugin.GetPlayerName(titles.Key));
                    otherTitlesDic.Add(titles.Value, sbb);
                }

            }
            foreach (KeyValuePair<String, StringBuilder> key in otherTitlesDic)
            {
                sb.AppendLine(key.Value.ToString());
            }
            sb.AppendLine("");
            sb.AppendLine("Hostile Factions and Hostile Alliances");
            foreach (String s in enemies)
            {
                sb.AppendLine(s);
            }

            foreach (long id in EnemyFactions)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag);
                }
            }
            sb.AppendLine("");
            sb.AppendLine("Member Factions");
            int memberCount = 0;
            foreach (long id in AllianceMembers)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Tag + " - " + fac.Members.Count + " members");
                    memberCount += fac.Members.Count;
                }
            }
            sb.AppendLine("Total Members " + memberCount);
            sb.AppendLine("");
            sb.AppendLine("Pending invites");
            foreach (long id in Invites)
            {
                IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                if (fac != null)
                {
                    sb.AppendLine(fac.Name + " - " + fac.Tag);
                }
            }

            return sb.ToString();
        }

        public void DoFriendlyUpdate(long firstId, long SecondId)
        {
            MyFactionStateChange change = MyFactionStateChange.SendFriendRequest;
            MyFactionStateChange change2 = MyFactionStateChange.AcceptFriendRequest;
            List<object[]> Input = new List<object[]>();
            object[] MethodInput = new object[] { change, firstId, SecondId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput);
            object[] MethodInput2 = new object[] { change2, SecondId, firstId, 0L };
            AlliancePlugin.sendChange?.Invoke(null, MethodInput2);

        }

        public void ForceFriendlies()
        {
            if (ForceFriends)
            {
                foreach (long id in AllianceMembers)
                {
                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                    if (fac != null)
                    {
                        foreach (long id2 in AllianceMembers)
                        {
                            IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                            if (fac2 != null && fac != fac2)
                            {
                                if (!MySession.Static.Factions.AreFactionsFriends(id, id2))
                                {
                                    MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 1500);
                                    DoFriendlyUpdate(id, id2);

                                }
                            }
                        }
                    }
                }
            }
        }

        public void ForceEnemies()
        {

            foreach (String s in enemies)
            {
                if (AlliancePlugin.AllAlliances.TryGetValue(s, out Alliance enemy))
                {
                    foreach (long id in AllianceMembers)
                    {
                        IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                        if (fac != null)
                        {

                            foreach (long id2 in enemy.AllianceMembers)
                            {
                                IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                                if (fac2 != null && fac != fac2)
                                {
                                    if (!MySession.Static.Factions.AreFactionsEnemies(id, id2))
                                    {
                                        MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 0);
                                        MyFactionCollection.DeclareWar(id, id2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            foreach (long id2 in EnemyFactions)
            {
                foreach (long id in AllianceMembers)
                {

                    IMyFaction fac = MySession.Static.Factions.TryGetFactionById(id);
                    IMyFaction fac2 = MySession.Static.Factions.TryGetFactionById(id2);
                    if (fac2 != null && fac != fac2)
                    {
                        if (!MySession.Static.Factions.AreFactionsEnemies(id, id2))
                        {
                            MySession.Static.Factions.SetReputationBetweenFactions(id, id2, 0);
                            MyFactionCollection.DeclareWar(id, id2);
                        }
                    }
                }
            }
        }
        public void ForceAddMember(long id)
        {
            if (!AllianceMembers.Contains(id))
            {
                AllianceMembers.Add(id);
            }
            ForceFriendlies();
        }
        public void SendInvite(long id)
        {

            if (!Invites.Contains(id))
            {
                Invites.Add(id);
            }

        }
        public void RevokeInvite(long id)
        {
            if (Invites.Contains(id))
            {
                Invites.Remove(id);
            }

        }
        public void RemoveTitle(ulong steamid, String title)
        {
            if (CustomRankPermissions.ContainsKey(title))
            {
                if (PlayersCustomRank.ContainsKey(steamid))
                {
                    PlayersCustomRank.Remove(steamid);
                    return;
                }

            }
            if (otherTitles.ContainsKey(steamid))
            {
                otherTitles.Remove(steamid);
                return;
            }
        }
        public void SetTitle(ulong steamid, String title)
        {
            if (CustomRankPermissions.ContainsKey(title))
            {
                if (PlayersCustomRank.ContainsKey(steamid))
                {
                    PlayersCustomRank[steamid] = title;
                }
                else
                {
                    PlayersCustomRank.Add(steamid, title);
                }
                return;
            }
            if (otherTitles.ContainsKey(steamid))
            {
                otherTitles[steamid] = title;
            }
            else
            {
                otherTitles.Add(steamid, title);
            }
        }
        public Boolean HasPermissionToInvite(ulong id)
        {
            if (SupremeLeader.Equals(id))
                return true;
            if (HasAccess(id, AccessLevel.Invite))
                return true;
            return false;
        }
        public Boolean JoinAlliance(MyFaction fac)
        {
            if (BlockedFactions.Contains(fac.FactionId))
            {
                return false;
            }
            if (Invites.Contains(fac.FactionId))
            {
                Invites.Remove(fac.FactionId);
                AllianceMembers.Remove(fac.FactionId);
                AllianceMembers.Add(fac.FactionId);
                ForceFriendlies();
                return true;
            }
            else
            {
                return false;
            }
        }

        public long GetBalance()
        {
            return this.bankBalance;
        }

    }
}
