using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;
using System.Threading;

namespace WOWS_Detonation_Counter
{
    public class Proxy
    {
        public static async Task<PlayerPersonalData> GetPlayerPersonalDataAsync(long account_id)
        {
            int repeatCount = 0;

            RESTART: PlayerPersonalData playerPersonalData = null;

            try
            {
                var url = String.Format("https://api.worldofwarships.asia/wows/account/info/?application_id=ff57f966d5a13e4a240ab7218b889a18&account_id={0}&extra=statistics.club%2Cstatistics.rank_div2%2Cstatistics.rank_div3%2Cstatistics.rank_solo", account_id.ToString());
                var http = new HttpClient();
                var playerPersonalDataSerializer = new DataContractJsonSerializer(typeof(PlayerPersonalData));
                var response = await http.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    repeatCount++;
                    Console.WriteLine("Fail to get playerPersonalData from API! response.StatusCode = " + response.StatusCode.ToString() + " Now go back to retry. Count:" + repeatCount);
                    Console.WriteWarning("Fail to get playerPersonalData from API! response.StatusCode = " + response.StatusCode.ToString() + " Now go back to retry. Count:" + repeatCount);
                    if (repeatCount >= 10)
                    {
                        goto SKIP;
                    }
                    else
                    {
                        goto RESTART;
                    }
                }
                var result = await response.Content.ReadAsStringAsync();

                //get hidden status
                if (Regex.IsMatch(result, String.Format("\"hidden\":\\[{0}\\]", account_id)))
                {
                    //replace hidden account_id with hidden
                    string playerHiddenStatusPattern = String.Format("\"hidden\":\\[{0}\\]", account_id);
                    Regex playerHiddenStatusRgx = new Regex(playerHiddenStatusPattern);
                    result = playerHiddenStatusRgx.Replace(result, "\"hidden\":\"hidden\"");
                }

                //replace account_id with playerPersonalDataDataData
                string playerPersonalDataDataPattern = String.Format("\"{0}\"", account_id);
                Regex playerPersonalDataDataRgx = new Regex(playerPersonalDataDataPattern);
                result = playerPersonalDataDataRgx.Replace(result, "\"playerPersonalDataDataData\"");

                var ms = new MemoryStream(Encoding.UTF8.GetBytes(result));
                playerPersonalData = (PlayerPersonalData)playerPersonalDataSerializer.ReadObject(ms);

                if (playerPersonalData.status != "ok")
                {
                    repeatCount++;
                    Console.WriteLine("Api return error! playerPersonalData.status = " + playerPersonalData.status + " Now go back to retry. Count:" + repeatCount);
                    Console.WriteWarning("Api return error! playerPersonalData.status = " + playerPersonalData.status + " Now go back to retry. Count:" + repeatCount);
                    if (repeatCount >= 10)
                    {
                        goto SKIP;
                    }
                    else
                    {
                        goto RESTART;
                    }
                }
            }
            catch (Exception e)
            {
                repeatCount++;
                Console.WriteLine("Exception detected! Details:" + e.Message + " Retry after 10 seconds... Count:" + repeatCount);
                Console.WriteWarning("Exception detected! Details:" + e.Message + " Retry after 10 seconds... Count:" + repeatCount);
                Thread.Sleep(10000);
                if (repeatCount >= 10)
                {
                    goto SKIP;
                }
                else
                {
                    goto RESTART;
                }
            }

            if (playerPersonalData == null)
            {
                repeatCount++;
                Console.WriteLine("Null player personal data detected! Now go back to retry. Count:" + repeatCount);
                Console.WriteWarning("Null player personal data detected! Now go back to retry. Count:" + repeatCount);
                if (repeatCount >= 10)
                {
                    goto SKIP;
                }
                else
                {
                    goto RESTART;
                }
            }

            return playerPersonalData;

            SKIP: playerPersonalData = new PlayerPersonalData();
            playerPersonalData.status = "skip";
            Console.WriteLine("Fail to get player personal data! Account_id:" + account_id + "! Now return skip playerPersonalData.");
            Console.WriteWarning("Fail to get player personal data! Account_id:" + account_id + "! Now return skip playerPersonalData.");
            return playerPersonalData;
        }

        public static async Task<PlayerAchievement> GetPlayerAchievementAsync(long account_id)
        {
            int repeatCount = 0;
            RESTART: PlayerAchievement playerAchievement = null;

            try
            {
                var url = String.Format("https://api.worldofwarships.asia/wows/account/achievements/?application_id=ff57f966d5a13e4a240ab7218b889a18&account_id={0}", account_id.ToString());
                var http = new HttpClient();
                var playerAchievementSerializer = new DataContractJsonSerializer(typeof(PlayerAchievement));
                var response = await http.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    repeatCount++;
                    Console.WriteLine("Fail to get playerAchievement from API! response.StatusCode = " + response.StatusCode.ToString() + " Now go back to retry. Count:" + repeatCount);
                    Console.WriteWarning("Fail to get playerAchievement from API! response.StatusCode = " + response.StatusCode.ToString() + " Now go back to retry. Count:" + repeatCount);
                    if (repeatCount >= 10)
                    {
                        goto SKIP;
                    }
                    else
                    {
                        goto RESTART;
                    }
                }
                var result = await response.Content.ReadAsStringAsync();

                //replace number with playerAchievementData
                string playerAchievementPattern = String.Format("\"{0}\"", account_id);
                Regex playerAchievementRgx = new Regex(playerAchievementPattern);
                result = playerAchievementRgx.Replace(result, "\"playerAchievementDataData\"");

                var ms = new MemoryStream(Encoding.UTF8.GetBytes(result));
                playerAchievement = (PlayerAchievement)playerAchievementSerializer.ReadObject(ms);

                if (playerAchievement.status != "ok")
                {
                    repeatCount++;
                    Console.WriteLine("Api return error! playerAchievement.status = " + playerAchievement.status + " Now go back to retry. Count:" + repeatCount);
                    Console.WriteWarning("Api return error! playerAchievement.status = " + playerAchievement.status + " Now go back to retry. Count:" + repeatCount);
                    if (repeatCount >= 10)
                    {
                        goto SKIP;
                    }
                    else
                    {
                        goto RESTART;
                    }
                }
            }
            catch (Exception e)
            {
                repeatCount++;
                Console.WriteLine("Exception detected! Details:" + e.Message + " Retry after 10 seconds... Count:" + repeatCount);
                Console.WriteWarning("Exception detected! Details:" + e.Message + " Retry after 10 seconds... Count:" + repeatCount);
                Thread.Sleep(10000);
                if (repeatCount >= 10)
                {
                    goto SKIP;
                }
                else
                {
                    goto RESTART;
                }
            }

            return playerAchievement;

            SKIP: playerAchievement = new PlayerAchievement();
            playerAchievement.status = "skip";
            Console.WriteLine("Fail to get player achievement! Account_id:" + account_id + "! Now return skip playerAchievement.");
            Console.WriteWarning("Fail to get player achievement! Account_id:" + account_id + "! Now return skip playerAchievement.");
            return playerAchievement;
        }
    }

    //player achievement part
    [DataContract]
    public class PlayerAchievementMeta
    {
        [DataMember]
        public int count { get; set; }

        [DataMember]
        public string hidden { get; set; }
    }

    [DataContract]
    public class Battle
    {
        [DataMember]
        public int BD2016_SNATCH { get; set; }

        [DataMember]
        public int FIGHTER { get; set; }

        [DataMember]
        public int CAMPAIGN_SB_COMPLETED { get; set; }

        [DataMember]
        public int BD2016_RUN_FOREST { get; set; }

        [DataMember]
        public int BD2_RANKS { get; set; }

        [DataMember]
        public int MILLIONAIR { get; set; }

        [DataMember]
        public int CLEAR_SKY { get; set; }

        [DataMember]
        public int CAMPAIGN_NY17B_COMPLETED { get; set; }

        [DataMember]
        public int SEA_LEGEND { get; set; }

        [DataMember]
        public int MESSENGER { get; set; }

        [DataMember]
        public int BD2_GB { get; set; }

        [DataMember]
        public int UNSINKABLE { get; set; }

        [DataMember]
        public int PVE_HON_PR_SAVE_1 { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_ARSONIST { get; set; }

        [DataMember]
        public int NY17_AIMING { get; set; }

        [DataMember]
        public int FIREPROOF { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_TACTICIAN { get; set; }

        [DataMember]
        public int MESSENGER_L { get; set; }

        [DataMember]
        public int WORKAHOLIC { get; set; }

        [DataMember]
        public int BATTLE_HERO { get; set; }

        [DataMember]
        public int BD2016_WRONG_SOW { get; set; }

        [DataMember]
        public int PVE_HON_WIN_ALL_DONE { get; set; }

        [DataMember]
        public int CAMPAIGN1_COMPLETED { get; set; }

        [DataMember]
        public int COLLECTION_DUNKIRK_COMPLETED { get; set; }

        [DataMember]
        public int BD2016_MANNERS { get; set; }

        [DataMember]
        public int NY17_DRESS_THE_TREE { get; set; }

        [DataMember]
        public int MAIN_CALIBER { get; set; }

        [DataMember]
        public int HEADBUTT { get; set; }

        [DataMember]
        public int INSTANT_KILL { get; set; }

        [DataMember]
        public int BD2_CAMPAIGNS { get; set; }

        [DataMember]
        public int CAMPAIGN_NY17B_COMPLETED_EXCELLENT { get; set; }

        [DataMember]
        public int JUNIOR_PLANNER { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_TO_THE_BOTTOM { get; set; }

        [DataMember]
        public int BD2_CREDITS { get; set; }

        [DataMember]
        public int PVE_DUNKERQUE_OPERATION_DYNAMO { get; set; }

        [DataMember]
        public int ENGINEER { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_HARD_EDGED { get; set; }

        [DataMember]
        public int NO_DAY_WITHOUT_ADVENTURE { get; set; }

        [DataMember]
        public int RETRIBUTION { get; set; }

        [DataMember]
        public int BD2_HW2016 { get; set; }

        [DataMember]
        public int MERCENARY_L { get; set; }

        [DataMember]
        public int PVE_HON_DONE_CLASS { get; set; }

        [DataMember]
        public int BD2016_PARTY_ANIMAL { get; set; }

        [DataMember]
        public int CAMPAIGN1_COMPLETED_EXCELLENT { get; set; }

        [DataMember]
        public int HALLOWEEN_2016 { get; set; }

        [DataMember]
        public int BD2_ARP { get; set; }

        [DataMember]
        public int VETERAN { get; set; }

        [DataMember]
        public int COLLECTION_BISMARCK_COMPLETED { get; set; }

        [DataMember]
        public int NEVER_ENOUGH_MONEY { get; set; }

        [DataMember]
        public int NY17_WIN_ALL { get; set; }

        [DataMember]
        public int NO_PRICE_FOR_HEROISM { get; set; }

        [DataMember]
        public int BD2_NY2016 { get; set; }

        [DataMember]
        public int BD2_PVE { get; set; }

        [DataMember]
        public int DREADNOUGHT { get; set; }

        [DataMember]
        public int CAPITAL { get; set; }

        [DataMember]
        public int BD2016_PARTY_CHECK_IN { get; set; }

        [DataMember]
        public int BD2016_FESTIV_SOUP { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_BOMBARDIER { get; set; }

        [DataMember]
        public int NY17_500_LEAGUES { get; set; }

        [DataMember]
        public int PVE_HON_FRAG_WAY { get; set; }

        [DataMember]
        public int DOUBLE_KILL { get; set; }

        [DataMember]
        public int WARRIOR { get; set; }

        [DataMember]
        public int NY17_TO_THE_BOTTOM { get; set; }

        [DataMember]
        public int FIRST_BLOOD { get; set; }

        [DataMember]
        public int DETONATED { get; set; }

        [DataMember]
        public int LIQUIDATOR { get; set; }

        [DataMember]
        public int NY17_SAFECRACKER { get; set; }

        [DataMember]
        public int AMAUTEUR { get; set; }

        [DataMember]
        public int BD2_CREW { get; set; }

        [DataMember]
        public int WITHERING { get; set; }

        [DataMember]
        public int CHIEF_ENGINEER { get; set; }

        [DataMember]
        public int BD2016_FIRESHOW { get; set; }

        [DataMember]
        public int NO_DAY_WITHOUT_ADVENTURE_L { get; set; }

        [DataMember]
        public int NY17_WIN_AT_LEAST_ONE { get; set; }

        [DataMember]
        public int ATBA_CALIBER { get; set; }

        [DataMember]
        public int CAMPAIGN_BISMARCK_COMPLETED { get; set; }

        [DataMember]
        public int BD2_CONTAINERS { get; set; }

        [DataMember]
        public int NY17_BREAK_THE_BANK { get; set; }

        [DataMember]
        public int MERCENARY { get; set; }

        [DataMember]
        public int COLLECTION_WOWSBIRTHDAY_COMPLETED { get; set; }

        [DataMember]
        public int SUPPORT { get; set; }

        [DataMember]
        public int PVE_HERO_DAM_ENEM { get; set; }

        [DataMember]
        public int ARSONIST { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_LUCKY { get; set; }
    }

    [DataContract]
    public class Progress
    {
        [DataMember]
        public int BATTLE_HERO { get; set; }

        [DataMember]
        public int WORKAHOLIC { get; set; }

        [DataMember]
        public int FIGHTER { get; set; }

        [DataMember]
        public int CHIEF_ENGINEER { get; set; }

        [DataMember]
        public int MILLIONAIR { get; set; }

        [DataMember]
        public int CAPITAL { get; set; }

        [DataMember]
        public int NO_DAY_WITHOUT_ADVENTURE { get; set; }

        [DataMember]
        public int MERCENARY_L { get; set; }

        [DataMember]
        public int NEVER_ENOUGH_MONEY { get; set; }

        [DataMember]
        public int NO_DAY_WITHOUT_ADVENTURE_L { get; set; }

        [DataMember]
        public int SEA_LEGEND { get; set; }

        [DataMember]
        public int NO_PRICE_FOR_HEROISM { get; set; }

        [DataMember]
        public int JUNIOR_PLANNER { get; set; }

        [DataMember]
        public int PVE_HON_PR_SAVE_1 { get; set; }

        [DataMember]
        public int MERCENARY { get; set; }

        [DataMember]
        public int MESSENGER { get; set; }

        [DataMember]
        public int SCIENCE_OF_WINNING_BOMBARDIER { get; set; }

        [DataMember]
        public int MESSENGER_L { get; set; }

        [DataMember]
        public int AMAUTEUR { get; set; }

        [DataMember]
        public int VETERAN { get; set; }

        [DataMember]
        public int ENGINEER { get; set; }
    }

    [DataContract]
    public class PlayerAchievementDataData
    {
        [DataMember]
        public Battle battle { get; set; }

        [DataMember]
        public Progress progress { get; set; }
    }

    [DataContract]
    public class PlayerAchievementData
    {
        [DataMember]
        public PlayerAchievementDataData playerAchievementDataData { get; set; }
    }

    [DataContract]
    public class PlayerAchievement
    {
        [DataMember]
        public string status { get; set; }

        [DataMember]
        public PlayerAchievementMeta meta { get; set; }

        [DataMember]
        public PlayerAchievementData data { get; set; }
    }

    //player personal data part
    [DataContract]
    public class PlayerPersonalDataMeta
    {
        [DataMember]
        public string count { get; set; }

        [DataMember]
        public string hidden { get; set; }
    }

    [DataContract]
    public class Club
    {
        [DataMember]
        public int max_xp { get; set; }

        [DataMember]
        public Main_battery main_battery { get; set; }

        [DataMember]
        public string max_ships_spotted_ship_id { get; set; }

        [DataMember]
        public int max_damage_scouting { get; set; }

        [DataMember]
        public string art_agro { get; set; }

        [DataMember]
        public string max_xp_ship_id { get; set; }

        [DataMember]
        public int ships_spotted { get; set; }

        [DataMember]
        public Second_battery second_battery { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public int xp { get; set; }

        [DataMember]
        public int survived_battles { get; set; }

        [DataMember]
        public int dropped_capture_points { get; set; }

        [DataMember]
        public int torpedo_agro { get; set; }

        [DataMember]
        public int draws { get; set; }

        [DataMember]
        public int control_captured_points { get; set; }

        [DataMember]
        public string max_total_agro_ship_id { get; set; }

        [DataMember]
        public int planes_killed { get; set; }

        [DataMember]
        public int battles { get; set; }

        [DataMember]
        public int max_ships_spotted { get; set; }

        [DataMember]
        public int survived_wins { get; set; }

        [DataMember]
        public int frags { get; set; }

        [DataMember]
        public int damage_scouting { get; set; }

        [DataMember]
        public int max_total_agro { get; set; }

        [DataMember]
        public int max_frags_battle { get; set; }

        [DataMember]
        public int capture_points { get; set; }

        [DataMember]
        public Ramming ramming { get; set; }

        [DataMember]
        public Torpedoes torpedoes { get; set; }

        [DataMember]
        public string max_planes_killed_ship_id { get; set; }

        [DataMember]
        public Aircraft aircraft { get; set; }

        [DataMember]
        public int team_capture_points { get; set; }

        [DataMember]
        public int control_dropped_points { get; set; }

        [DataMember]
        public int max_damage_dealt { get; set; }

        [DataMember]
        public string max_damage_dealt_ship_id { get; set; }

        [DataMember]
        public int wins { get; set; }

        [DataMember]
        public int losses { get; set; }

        [DataMember]
        public int damage_dealt { get; set; }

        [DataMember]
        public int max_planes_killed { get; set; }

        [DataMember]
        public string max_scouting_damage_ship_id { get; set; }

        [DataMember]
        public int team_dropped_capture_points { get; set; }
    }

    [DataContract]
    public class Main_battery
    {
        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string hits { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public string shots { get; set; }
    }

    [DataContract]
    public class Second_battery
    {
        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string hits { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public string shots { get; set; }
    }

    [DataContract]
    public class Ramming
    {
        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }
    }

    [DataContract]
    public class Torpedoes
    {
        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string hits { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public string shots { get; set; }
    }

    [DataContract]
    public class Aircraft
    {
        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }
    }

    [DataContract]
    public class Rank_solo
    {
        [DataMember]
        public int max_xp { get; set; }

        [DataMember]
        public Main_battery main_battery { get; set; }

        [DataMember]
        public string max_ships_spotted_ship_id { get; set; }

        [DataMember]
        public int max_damage_scouting { get; set; }

        [DataMember]
        public string art_agro { get; set; }

        [DataMember]
        public string max_xp_ship_id { get; set; }

        [DataMember]
        public int ships_spotted { get; set; }

        [DataMember]
        public Second_battery second_battery { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public int xp { get; set; }

        [DataMember]
        public int survived_battles { get; set; }

        [DataMember]
        public int dropped_capture_points { get; set; }

        [DataMember]
        public int torpedo_agro { get; set; }

        [DataMember]
        public int draws { get; set; }

        [DataMember]
        public int control_captured_points { get; set; }

        [DataMember]
        public string max_total_agro_ship_id { get; set; }

        [DataMember]
        public int planes_killed { get; set; }

        [DataMember]
        public int battles { get; set; }

        [DataMember]
        public int max_ships_spotted { get; set; }

        [DataMember]
        public int survived_wins { get; set; }

        [DataMember]
        public int frags { get; set; }

        [DataMember]
        public int damage_scouting { get; set; }

        [DataMember]
        public int max_total_agro { get; set; }

        [DataMember]
        public int max_frags_battle { get; set; }

        [DataMember]
        public int capture_points { get; set; }

        [DataMember]
        public Ramming ramming { get; set; }

        [DataMember]
        public Torpedoes torpedoes { get; set; }

        [DataMember]
        public string max_planes_killed_ship_id { get; set; }

        [DataMember]
        public Aircraft aircraft { get; set; }

        [DataMember]
        public int team_capture_points { get; set; }

        [DataMember]
        public int control_dropped_points { get; set; }

        [DataMember]
        public int max_damage_dealt { get; set; }

        [DataMember]
        public string max_damage_dealt_ship_id { get; set; }

        [DataMember]
        public int wins { get; set; }

        [DataMember]
        public int losses { get; set; }

        [DataMember]
        public int damage_dealt { get; set; }

        [DataMember]
        public int max_planes_killed { get; set; }

        [DataMember]
        public string max_scouting_damage_ship_id { get; set; }

        [DataMember]
        public int team_dropped_capture_points { get; set; }
    }

    [DataContract]
    public class Pvp
    {
        [DataMember]
        public string max_xp { get; set; }

        [DataMember]
        public string damage_to_buildings { get; set; }

        [DataMember]
        public Main_battery main_battery { get; set; }

        [DataMember]
        public string max_ships_spotted_ship_id { get; set; }

        [DataMember]
        public string max_damage_scouting { get; set; }

        [DataMember]
        public string art_agro { get; set; }

        [DataMember]
        public string max_xp_ship_id { get; set; }

        [DataMember]
        public string ships_spotted { get; set; }

        [DataMember]
        public Second_battery second_battery { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public string xp { get; set; }

        [DataMember]
        public string survived_battles { get; set; }

        [DataMember]
        public string dropped_capture_points { get; set; }

        [DataMember]
        public string max_damage_dealt_to_buildings { get; set; }

        [DataMember]
        public string torpedo_agro { get; set; }

        [DataMember]
        public string draws { get; set; }

        [DataMember]
        public string control_captured_points { get; set; }

        [DataMember]
        public string max_total_agro_ship_id { get; set; }

        [DataMember]
        public string planes_killed { get; set; }

        [DataMember]
        public string battles { get; set; }

        [DataMember]
        public string max_ships_spotted { get; set; }

        [DataMember]
        public string max_suppressions_ship_id { get; set; }

        [DataMember]
        public string survived_wins { get; set; }

        [DataMember]
        public string frags { get; set; }

        [DataMember]
        public string damage_scouting { get; set; }

        [DataMember]
        public string max_total_agro { get; set; }

        [DataMember]
        public string max_frags_battle { get; set; }

        [DataMember]
        public string capture_points { get; set; }

        [DataMember]
        public Ramming ramming { get; set; }

        [DataMember]
        public string suppressions_count { get; set; }

        [DataMember]
        public string max_suppressions_count { get; set; }

        [DataMember]
        public Torpedoes torpedoes { get; set; }

        [DataMember]
        public string max_planes_killed_ship_id { get; set; }

        [DataMember]
        public Aircraft aircraft { get; set; }

        [DataMember]
        public string team_capture_points { get; set; }

        [DataMember]
        public string control_dropped_points { get; set; }

        [DataMember]
        public string max_damage_dealt { get; set; }

        [DataMember]
        public string max_damage_dealt_to_buildings_ship_id { get; set; }

        [DataMember]
        public string max_damage_dealt_ship_id { get; set; }

        [DataMember]
        public string wins { get; set; }

        [DataMember]
        public string losses { get; set; }

        [DataMember]
        public string damage_dealt { get; set; }

        [DataMember]
        public string max_planes_killed { get; set; }

        [DataMember]
        public string max_scouting_damage_ship_id { get; set; }

        [DataMember]
        public string team_dropped_capture_points { get; set; }

        [DataMember]
        public string battles_since_512 { get; set; }
    }

    [DataContract]
    public class Rank_div3
    {
        [DataMember]
        public int max_xp { get; set; }

        [DataMember]
        public Main_battery main_battery { get; set; }

        [DataMember]
        public string max_ships_spotted_ship_id { get; set; }

        [DataMember]
        public int max_damage_scouting { get; set; }

        [DataMember]
        public string art_agro { get; set; }

        [DataMember]
        public string max_xp_ship_id { get; set; }

        [DataMember]
        public int ships_spotted { get; set; }

        [DataMember]
        public Second_battery second_battery { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public int xp { get; set; }

        [DataMember]
        public int survived_battles { get; set; }

        [DataMember]
        public int dropped_capture_points { get; set; }

        [DataMember]
        public int torpedo_agro { get; set; }

        [DataMember]
        public int draws { get; set; }

        [DataMember]
        public int control_captured_points { get; set; }

        [DataMember]
        public string max_total_agro_ship_id { get; set; }

        [DataMember]
        public int planes_killed { get; set; }

        [DataMember]
        public int battles { get; set; }

        [DataMember]
        public int max_ships_spotted { get; set; }

        [DataMember]
        public int survived_wins { get; set; }

        [DataMember]
        public int frags { get; set; }

        [DataMember]
        public int damage_scouting { get; set; }

        [DataMember]
        public int max_total_agro { get; set; }

        [DataMember]
        public int max_frags_battle { get; set; }

        [DataMember]
        public int capture_points { get; set; }

        [DataMember]
        public Ramming ramming { get; set; }

        [DataMember]
        public Torpedoes torpedoes { get; set; }

        [DataMember]
        public string max_planes_killed_ship_id { get; set; }

        [DataMember]
        public Aircraft aircraft { get; set; }

        [DataMember]
        public int team_capture_points { get; set; }

        [DataMember]
        public int control_dropped_points { get; set; }

        [DataMember]
        public int max_damage_dealt { get; set; }

        [DataMember]
        public string max_damage_dealt_ship_id { get; set; }

        [DataMember]
        public int wins { get; set; }

        [DataMember]
        public int losses { get; set; }

        [DataMember]
        public int damage_dealt { get; set; }

        [DataMember]
        public int max_planes_killed { get; set; }

        [DataMember]
        public string max_scouting_damage_ship_id { get; set; }

        [DataMember]
        public int team_dropped_capture_points { get; set; }
    }

    [DataContract]
    public class Rank_div2
    {
        [DataMember]
        public int max_xp { get; set; }

        [DataMember]
        public Main_battery main_battery { get; set; }

        [DataMember]
        public string max_ships_spotted_ship_id { get; set; }

        [DataMember]
        public int max_damage_scouting { get; set; }

        [DataMember]
        public string art_agro { get; set; }

        [DataMember]
        public string max_xp_ship_id { get; set; }

        [DataMember]
        public int ships_spotted { get; set; }

        [DataMember]
        public Second_battery second_battery { get; set; }

        [DataMember]
        public string max_frags_ship_id { get; set; }

        [DataMember]
        public int xp { get; set; }

        [DataMember]
        public int survived_battles { get; set; }

        [DataMember]
        public int dropped_capture_points { get; set; }

        [DataMember]
        public int torpedo_agro { get; set; }

        [DataMember]
        public int draws { get; set; }

        [DataMember]
        public int control_captured_points { get; set; }

        [DataMember]
        public string max_total_agro_ship_id { get; set; }

        [DataMember]
        public int planes_killed { get; set; }

        [DataMember]
        public int battles { get; set; }

        [DataMember]
        public int max_ships_spotted { get; set; }

        [DataMember]
        public int survived_wins { get; set; }

        [DataMember]
        public int frags { get; set; }

        [DataMember]
        public int damage_scouting { get; set; }

        [DataMember]
        public int max_total_agro { get; set; }

        [DataMember]
        public int max_frags_battle { get; set; }

        [DataMember]
        public int capture_points { get; set; }

        [DataMember]
        public Ramming ramming { get; set; }

        [DataMember]
        public Torpedoes torpedoes { get; set; }

        [DataMember]
        public string max_planes_killed_ship_id { get; set; }

        [DataMember]
        public Aircraft aircraft { get; set; }

        [DataMember]
        public int team_capture_points { get; set; }

        [DataMember]
        public int control_dropped_points { get; set; }

        [DataMember]
        public int max_damage_dealt { get; set; }

        [DataMember]
        public string max_damage_dealt_ship_id { get; set; }

        [DataMember]
        public int wins { get; set; }

        [DataMember]
        public int losses { get; set; }

        [DataMember]
        public int damage_dealt { get; set; }

        [DataMember]
        public int max_planes_killed { get; set; }

        [DataMember]
        public string max_scouting_damage_ship_id { get; set; }

        [DataMember]
        public int team_dropped_capture_points { get; set; }
    }

    [DataContract]
    public class Statistics
    {
        [DataMember]
        public string distance { get; set; }

        [DataMember]
        public string battles { get; set; }

        [DataMember]
        public Pvp pvp { get; set; }

        [DataMember]
        public Club club { get; set; }

        [DataMember]
        public Rank_solo rank_solo { get; set; }

        [DataMember]
        public Rank_div3 rank_div3 { get; set; }

        [DataMember]
        public Rank_div2 rank_div2 { get; set; }
    }

    [DataContract]
    public class PlayerPersonalDataDataData
    {
        [DataMember]
        public string last_battle_time { get; set; }

        [DataMember]
        public string account_id { get; set; }

        [DataMember]
        public string leveling_tier { get; set; }

        [DataMember]
        public string created_at { get; set; }

        [DataMember]
        public string leveling_points { get; set; }

        [DataMember]
        public string updated_at { get; set; }

        [DataMember]
        public string private_ { get; set; }

        [DataMember]
        public string hidden_profile { get; set; }

        [DataMember]
        public string logout_at { get; set; }

        [DataMember]
        public string karma { get; set; }

        [DataMember]
        public Statistics statistics { get; set; }

        [DataMember]
        public string nickname { get; set; }

        [DataMember]
        public string stats_updated_at { get; set; }
    }

    [DataContract]
    public class PlayerPersonalDataData
    {
        [DataMember]
        public PlayerPersonalDataDataData playerPersonalDataDataData { get; set; }
    }

    [DataContract]
    public class PlayerPersonalData
    {
        [DataMember]
        public string status { get; set; }

        [DataMember]
        public PlayerPersonalDataMeta meta { get; set; }

        [DataMember]
        public PlayerPersonalDataData data { get; set; }
    }

}
