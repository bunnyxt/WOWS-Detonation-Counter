using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Net.Mail;
using WOWS_Detonation_Counter;
using System.Net;

namespace WOWS_Detonation_Counter
{
    class Program
    {
        //execution config
        static Config config;

        static DateTime startTime;
        static DateTime endTime;

        static void Main(string[] args)
        {
            MySqlConnection myConn;

            startTime = DateTime.Now;

            Console.WriteLine("WOWS Detonation Counter");
            Console.WriteLine("by bunnyxt 2018-06-04");
            Console.WriteLine("start time : " + startTime.ToString());
            Console.WriteLine();

            //load config from ./config.json
            Console.WriteLine("Now load config from ./config.json...");
            FileStream jsonFile;
            byte[] jsonFileBytes;
            string jsonString;
            try
            {
                jsonFile = new FileStream("./config.json", FileMode.Open, FileAccess.Read);
                jsonFileBytes = new byte[jsonFile.Length];
                jsonFile.Read(jsonFileBytes, 0, (int)jsonFile.Length);
                jsonString = Encoding.Default.GetString(jsonFileBytes);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning(e.Message);
                throw;
            }
            config = Config.FromJson(jsonString);
            Console.WriteLine("Config loaded from ./config.json!");
            Console.WriteLine();

            //show config
            Console.WriteLine("date:\t\t" + config.Date);
            Console.WriteLine("mySQLDatabase:");
            Console.WriteLine("server:\t\t" + config.MySqlDatabase.Server);
            Console.WriteLine("userId:\t\t" + config.MySqlDatabase.UserId);
            Console.WriteLine("password:\t" + config.MySqlDatabase.Password);
            Console.WriteLine("database:\t" + config.MySqlDatabase.Database);
            Console.WriteLine("mode:\t\t" + config.Mode);
            switch (config.Mode)
            {
                case 1:
                    Console.WriteLine("mode1:");
                    Console.WriteLine("targetSum:\t" + config.Mode1.TargetSum);
                    Console.WriteLine("startAccountId:\t" + config.Mode1.StartAccountId);
                    break;
                case 2:
                    Console.WriteLine("mode2:");
                    Console.WriteLine("rangeMin:\t" + config.Mode2.RangeMin);
                    Console.WriteLine("rangeMax:\t" + config.Mode2.RangeMax);
                    break;
                case 3:
                    Console.WriteLine("mode3:");
                    Console.WriteLine("AccountId:\t" + config.Mode3.AccountId);
                    break;
                //case 999:
                //    SendMail("SubjectTest","BodyTest");
                //    break;
                default:
                    Console.WriteLine("Invalid mode id " + config.Mode + " !");
                    Console.WriteWarning("Invalid mode id " + config.Mode + " !");
                    MessageBox.Show("Invalid mode id " + config.Mode + " !", "Error!");
                    throw new Exception();
            }
            Console.WriteLine();

            //connect to database
            try
            {
                myConn = GetMySqlConnection();
                Console.WriteLine("Now connecting database...");
                myConn.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning("Cannot connect to database! Details:" + e.Message);
                MessageBox.Show("Cannot connect to database!\nDetails:" + e.Message, "Error!");
                throw;
            }
            Console.WriteLine("MySQL database connection succeed!");
            Console.WriteLine();

            //select mode
            Console.WriteLine("Select mode:");
            Console.WriteLine("1 for finding new users (one by one)");
            Console.WriteLine("2 for updating now exist users");
            Console.WriteLine("3 for inserting one user via account_id");
            Console.WriteLine();
            Thread.Sleep(1000);

            Console.WriteLine("Mode " + config.Mode + " selected.");
            switch (config.Mode)
            {
                case 1:
                    FindNewUsers(myConn);
                    break;
                case 2:
                    UpdateExistUsers(myConn);
                    break;
                case 3:
                    AddNewUserViaAccountId(myConn);
                    break;
                default:
                    Console.WriteLine("Invalid mode id " + config.Mode + " !");
                    Console.WriteWarning("Invalid mode id " + config.Mode + " !");
                    MessageBox.Show("Invalid mode id " + config.Mode + " !", "Error!");
                    throw new Exception();
            }

            Console.ReadLine();

            //close database
            try
            {
                Console.WriteLine("Now closing database...");
                myConn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning("Cannot close database! Details:" + e.Message);
                MessageBox.Show("Cannot close database!\nDetails:" + e.Message, "Error!");
                throw;
            }
            Console.WriteLine("close succeed!");
            Console.WriteLine();

            //done
            MessageBox.Show("Done!", "Notice");

        }

        public static MySqlConnection GetMySqlConnection()
        {
            string server = config.MySqlDatabase.Server;
            string userId = config.MySqlDatabase.UserId;
            string password = config.MySqlDatabase.Password;
            string database = config.MySqlDatabase.Database;
            string connStr = String.Format("server={0};User Id={1};password={2};Database={3}", server, userId, password, database);

            Console.WriteLine("MySqlConnection\t:\t");
            Console.WriteLine("server\t\t:\t" + server);
            Console.WriteLine("userId\t\t:\t" + userId);
            Console.WriteLine("password\t:\t" + password);
            Console.WriteLine("database\t:\t" + database);
            Console.WriteLine();

            return new MySqlConnection(connStr);
        }

        public static async void FindNewUsers(MySqlConnection myConn)
        {
            //mysql components
            MySqlCommand myCmd;
            MySqlDataReader myRdr;

            //operation instances
            PlayerAchievement playerAchievementData = null;
            PlayerPersonalData playerPersonalDataData = null;

            //sql manage data

            int id, existedMaxId = 0, count = 0, nullCount = 0, detoSum = -1, btleSum = -1;
            long account_id = 0;
            string username = "";
            bool isHidden = false;

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY id DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxId = myRdr.GetInt32(0);
                    account_id = myRdr.GetInt64(1);
                }
                myRdr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning("MySQL execution(s) ran into an error! Details:" + e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

            //load execute range
            int targetSum;
            Console.WriteLine("Now max id is " + existedMaxId);
            Console.WriteLine("Load target new user sum number:");
            targetSum = config.Mode1.TargetSum;
            Console.WriteLine(targetSum);
            if (targetSum > 0)
            {
                Console.WriteLine("Now start getting " + targetSum + " new users...");
            }
            else
            {
                Console.WriteLine("Invalid target new user sum number " + config.Mode1.TargetSum + " !");
                Console.WriteWarning("Invalid target new user sum number " + config.Mode1.TargetSum + " !");
                MessageBox.Show("Invalid target new user sum number " + config.Mode1.TargetSum + " !", "Error!");
                throw new Exception();
            }

            //getting new users
            id = existedMaxId;
            account_id++;

            //modify
            if (config.Mode1.StartAccountId != -1)
            {
                Console.WriteLine("Change StartAccountId to " + config.Mode1.StartAccountId + " .");
                account_id = config.Mode1.StartAccountId;
            }

            while (count < targetSum)
            {
                Console.WriteLine("account_id:" + account_id);

                //initialize data
                detoSum = -1; btleSum = -1;
                username = "";
                isHidden = false;

                //get player personal data
                RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                //check skip status
                if (playerPersonalDataData.status == "skip")
                {
                    Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                    Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                    continue;
                }

                //check account_id existed or not
                if (playerPersonalDataData.data.playerPersonalDataDataData == null)
                {
                    Console.WriteLine("null  " + (++nullCount));
                    Console.WriteLine("");

                    if (nullCount == 1000)
                    {
                        Console.WriteLine("1000 invalid account_id passed!");
                        endTime = DateTime.Now;
                        Console.WriteLine("end time : " + endTime.ToString());
                        Console.WriteLine();
                        //TODO  account_id - 1000?? or - 1001??
                        SendMail("Mode 1 Finihed!", "1000 invalid acount_id passed! Mow max account_id is " + (account_id - 1000) + ", max id is " + id + ", start time : " + startTime.ToString() + ", end time : " + endTime.ToString() + ".");
                        break;
                    }

                    account_id++;
                    continue;
                }

                username = playerPersonalDataData.data.playerPersonalDataDataData.nickname;

                //check hidden status
                if (playerPersonalDataData.meta.hidden == "hidden")
                {
                    isHidden = true;

                    //insert hidden status
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '1')", id + 1, account_id, username), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {
                            Console.WriteLine("Hidden!");
                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user simple info to asia_player!");
                            MessageBox.Show("Fail to insert new user simple info to asia_player!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }

                    //initialize battle sum and deto sum table with 0
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }
                }
                else
                {
                    isHidden = false;

                    //insert not hidden status
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '0')", id + 1, account_id, username), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user simple info to asia_player!");
                            MessageBox.Show("Fail to insert new user simple info to asia_player!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }

                    //get player achievement data
                    playerAchievementData = await Proxy.GetPlayerAchievementAsync(account_id);

                    //check skip status
                    if (playerAchievementData.status == "skip")
                    {
                        Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                        Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                        continue;
                    }

                    //calculate battle sum and deto sum
                    btleSum =
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                    detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;

                    //initialize battle sum and deto sum table with 0
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }

                    //initialize other table with 0
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("" +
                        "INSERT INTO `wows_detonation`.`asia_deto_total_rank` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_btle_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period_rank` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_total_ratio` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_total_ratio_rank` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period_ratio` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period_ratio_rank` (`id`) VALUES ('{0}');",
                        id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to initialize other table!");
                            MessageBox.Show("Fail to initialize other table!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }

                    //update battle sum and deto sum for trigger
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_btle_total` SET `{0}`='{1}' WHERE `id`='{2}';", config.Date, btleSum, id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                        myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_deto_total` SET `{0}`='{1}' WHERE `id`='{2}';", config.Date, detoSum, id + 1), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                        Console.WriteLine("Retry after 10 seconds...");
                        Thread.Sleep(10000);
                        goto RESTART;
                    }

                }

                Console.WriteLine(
                    "id:\t" + (id + 1) + "\t" +
                    "nickname:\t" + username + "\t" +
                    "btleSum:\t" + btleSum + "\t" +
                    "detoSum:\t" + detoSum + "\t" +
                    "nullCount:\t" + nullCount + "\t"
                    );
                nullCount = 0;
                Console.WriteLine("");

                id++;
                count++;
                account_id++;
            }
        }

        public static async void UpdateExistUsers(MySqlConnection myConn)
        {
            //mysql components
            MySqlConnection myCon;
            MySqlCommand myCmd;
            MySqlDataReader myRdr;

            //operation instances
            PlayerAchievement playerAchievementData = null;
            PlayerPersonalData playerPersonalDataData = null;

            //sql manage data
            //string date = "Y17W44";

            int id, existedMaxId = 0, detoSum = -1, btleSum = -1;
            long account_id = 0;
            string username = "";
            bool isHidden = false;

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY id DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxId = myRdr.GetInt32(0);
                }
                myRdr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

            //load execute range
            int rangeMin, rangeMax;
            Console.WriteLine("Now max id is " + existedMaxId);
            Console.WriteLine("Load update id range:");
            rangeMin = config.Mode2.RangeMin;
            rangeMax = config.Mode2.RangeMax;
            Console.WriteLine(rangeMin);
            Console.WriteLine(rangeMax);
            if (rangeMin <= rangeMax && rangeMin >= 1 && rangeMax <= existedMaxId)
            {
                Console.WriteLine("Now start updating user " + rangeMin + " - " + rangeMax + "...");
            }
            else
            {
                Console.WriteLine("Invalid update id range min:" + rangeMin + " max:" + rangeMax + "!");
                Console.WriteWarning("Invalid update id range min:" + rangeMin + " max:" + rangeMax + "!");
                MessageBox.Show("Invalid update id range min:" + rangeMin + " max:" + rangeMax + "!", "Error!");
                throw new Exception();
            }

            //update selected users
            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player WHERE id >= " + rangeMin + " && id <= " + rangeMax + " ORDER BY id;", myConn);
                myCmd.CommandTimeout = 60;//60s timeout
                myRdr = myCmd.ExecuteReader();

                //connect to database for inner usage
                try
                {
                    myCon = GetMySqlConnection();
                    Console.WriteLine("now connecting database...");
                    myCon.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    MessageBox.Show("Cannot connect to database!\nDetails:" + e.Message, "Error!");
                    throw;
                }
                Console.WriteLine("connection succeed!");
                Console.WriteLine();

                //get user profile before
                int i = 0;
                //range limit 300000
                int[] ids = new int[300000];
                long[] accountIds = new long[300000];
                string[] usernames = new string[300000];
                bool[] isHiddens = new bool[300000];
                while (myRdr.Read())
                {
                    ids[i] = myRdr.GetInt32(0);
                    accountIds[i] = myRdr.GetInt64(1);
                    usernames[i] = myRdr.GetString(2);
                    isHiddens[i] = myRdr.GetBoolean(3);
                    i++;
                }
                int userNum = i;
                myRdr.Close();

                //inner update
                i = 0;
                for (i = 0; i < userNum; i++)
                {
                    //initialize data
                    detoSum = -1; btleSum = -1;
                    username = "";
                    isHidden = false;

                    //read from array
                    id = ids[i];
                    account_id = accountIds[i];
                    username = usernames[i];
                    isHidden = isHiddens[i];

                    //get personal data for personal information and battle number
                    RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                    //check skip status
                    if (playerPersonalDataData.status == "skip")
                    {
                        Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                        Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                        SendMail("Skip status detected!", DateTime.Now.ToString() + "  playerPersonalDataData.status == \"skip\"  Now skip id:" + id + " account_id:" + account_id + "!");
                        continue;
                    }

                    //check hidden status
                    if (isHidden == false && playerPersonalDataData.meta.hidden == "hidden")
                    {
                        //change from not hidden to hidden
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_player` SET `is_hidden`='1' WHERE `id`='{1}'", playerPersonalDataData.data.playerPersonalDataDataData.nickname, id), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {
                                Console.WriteLine("Changed from not hidden to hidden");
                            }
                            else
                            {
                                Console.WriteLine("Fail to change from not hidden to hidden in aisa_player!");
                                MessageBox.Show("MySQL UPDATE command error!\nFail to change from not hidden to hidden in aisa_player!", "Error!");
                                goto RESTART;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                            Console.WriteLine("Retry after 10 seconds...");
                            Thread.Sleep(10000);
                            goto RESTART;
                        }

                    }
                    if (isHidden == true && playerPersonalDataData.meta.hidden == null)
                    {
                        //change from hidden to not hidden
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_player` SET `is_hidden`='0' WHERE `id`='{1}'", playerPersonalDataData.data.playerPersonalDataDataData.nickname, id), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {
                                Console.WriteLine("Changed from hidden to not hidden");
                            }
                            else
                            {
                                Console.WriteLine("Fail to change from hidden to not hidden in aisa_player!");
                                MessageBox.Show("MySQL UPDATE command error!\nFail to change from hidden to not hidden in aisa_player!", "Error!");
                                goto RESTART;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                            Console.WriteLine("Retry after 10 seconds...");
                            Thread.Sleep(10000);
                            goto RESTART;
                        }

                    }

                    //update username
                    if (username != playerPersonalDataData.data.playerPersonalDataDataData.nickname)
                    {
                        username = playerPersonalDataData.data.playerPersonalDataDataData.nickname;
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_player` SET `user_name`='{0}' WHERE `id`='{1}'", username, id), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {
                                Console.WriteLine("Username updated!");
                            }
                            else
                            {
                                Console.WriteLine("Fail to update username in aisa_player!");
                                MessageBox.Show("MySQL UPDATE command error!\nFail to update username in aisa_player!", "Error!");
                                goto RESTART;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                            Console.WriteLine("Retry after 10 seconds...");
                            Thread.Sleep(10000);
                            goto RESTART;
                        }
                    }

                    //not hidden player need to update their battle and deto number
                    if (playerPersonalDataData.meta.hidden != "hidden")
                    {
                        //get player achievement data
                        playerAchievementData = await Proxy.GetPlayerAchievementAsync(account_id);

                        //check skip status
                        if (playerAchievementData.status == "skip")
                        {
                            Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                            Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                            SendMail("Skip status detected!", DateTime.Now.ToString() + "  playerAchievementData.status == \"skip\"  Now skip id:" + id + " account_id:" + account_id + "!");
                            continue;
                        }

                        //update battle sum and deto sum
                        //update asia_btle_total
                        btleSum =
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_btle_total` SET `{0}`='{1}' WHERE `id`='{2}'", config.Date, btleSum, id), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to update battle sum in asia_btle_total!");
                                MessageBox.Show("MySQL UPDATE command error!\nFail to update battle sum in asia_btle_total!", "Error!");
                                goto RESTART;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                            Console.WriteLine("Retry after 10 seconds...");
                            Thread.Sleep(10000);
                            goto RESTART;
                        }

                        //update asia_deto_total
                        detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_deto_total` SET `{0}`='{1}' WHERE `id`='{2}'", config.Date, detoSum, id), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to update deto sum in asia_btle_total!");
                                MessageBox.Show("MySQL UPDATE command error!\nFail to update deto sum in asia_btle_total!", "Error!");
                                goto RESTART;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                            Console.WriteLine("Retry after 10 seconds...");
                            Thread.Sleep(10000);
                            goto RESTART;
                        }

                    }

                    //show result
                    Console.WriteLine(
                        "id:\t\t" + id + "\t" +
                        "account_id:\t" + account_id + "\t\n" +
                        "is_hidden:\t" + isHidden + "\t" +
                        "nickname:\t" + username + "\t\n" +
                        "btleSum:\t" + btleSum + "\t" +
                        "detoSum:\t" + detoSum + "\t"
                        );
                    Console.WriteLine();
                }

                //close database
                try
                {
                    Console.WriteLine("now closing database...");
                    myCon.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    MessageBox.Show("Cannot close database!\nDetails:" + e.Message, "Error!");
                    throw;
                }
                Console.WriteLine("close succeed!");
                Console.WriteLine();

                endTime = DateTime.Now;
                Console.WriteLine("end time : " + endTime.ToString());
                Console.WriteLine();
                SendMail("Mode 2 Finihed!", "RangeMin:" + config.Mode2.RangeMin + " RangeMax:" + config.Mode2.RangeMax + " start time : " + startTime.ToString() + " end time : " + endTime.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

        }

        public static void SendMail(string subject, string body)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(config.Mail.Sender);
                mailMessage.To.Add(new MailAddress(config.Mail.Receiver));
                mailMessage.Subject = config.Tag + " : " + subject;
                mailMessage.Body = body + "  From:" + config.Tag;
                SmtpClient client = new SmtpClient();
                client.Host = config.Mail.ClientHost;
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(config.Mail.Sender, config.Mail.CreditCode);
                client.Send(mailMessage);
                Console.WriteLine("Mail sent succeed! \nSubject:" + subject + " \nBody:" + body);
                Console.WriteWarning("Mail sent succeed! \r\nSubject:" + subject + " \r\nBody:" + body);
            }
            catch (Exception e)
            {
                Console.WriteLine("Mail sent exception detected! Details:" + e.Message);
                Console.WriteWarning("Mail sent exception detected! Details:" + e.Message);
                return;
            }
        }

        public static async void AddNewUserViaAccountId(MySqlConnection myConn)
        {
            //mysql components
            MySqlCommand myCmd;
            MySqlDataReader myRdr;

            //operation instances
            PlayerAchievement playerAchievementData = null;
            PlayerPersonalData playerPersonalDataData = null;

            //sql manage data

            int id, existedMaxId = 0, count = 0, nullCount = 0, detoSum = -1, btleSum = -1;
            long account_id = 0;
            string username = "";
            bool isHidden = false;

            //load target accountId
            long targetAccountId = config.Mode3.AccountId;
            Console.WriteLine("Target Account Id is " + targetAccountId);

            //check whether accountId existed
            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player where account_id = " + targetAccountId + ";", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    Console.WriteLine("AccountId " + targetAccountId + " already exist!");
                    Console.WriteLine(myRdr.GetInt32(0).ToString() + " " + myRdr.GetInt64(1).ToString() + " " + myRdr.GetString(2) + " " + myRdr.GetBoolean(3).ToString());
                    myRdr.Close();
                    return;
                }
                myRdr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning("MySQL execution(s) ran into an error! Details:" + e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY id DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxId = myRdr.GetInt32(0);
                    account_id = myRdr.GetInt64(1);
                }
                myRdr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteWarning("MySQL execution(s) ran into an error! Details:" + e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

            id = existedMaxId;
            account_id = targetAccountId;

            //getting target user
            Console.WriteLine("account_id:" + account_id);

            //initialize data
            detoSum = -1; btleSum = -1;
            username = "";
            isHidden = false;

            //get player personal data
            RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

            //check skip status
            if (playerPersonalDataData.status == "skip")
            {
                Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                return;
            }

            username = playerPersonalDataData.data.playerPersonalDataDataData.nickname;

            //check hidden status
            if (playerPersonalDataData.meta.hidden == "hidden")
            {
                isHidden = true;

                //insert hidden status
                try
                {
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '1')", id + 1, account_id, username), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {
                        Console.WriteLine("Hidden!");
                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user simple info to asia_player!");
                        MessageBox.Show("Fail to insert new user simple info to asia_player!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }

                //initialize battle sum and deto sum table with 0
                try
                {
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }
            }
            else
            {
                isHidden = false;

                //insert not hidden status
                try
                {
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '0')", id + 1, account_id, username), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user simple info to asia_player!");
                        MessageBox.Show("Fail to insert new user simple info to asia_player!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }

                //get player achievement data
                playerAchievementData = await Proxy.GetPlayerAchievementAsync(account_id);

                //check skip status
                if (playerAchievementData.status == "skip")
                {
                    Console.WriteLine("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                    Console.WriteWarning("Skip status detected! Now skip id:" + id + " account_id:" + account_id + "!");
                    return;
                }

                //calculate battle sum and deto sum
                btleSum =
                        Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                        Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                        Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                        Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                        Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;

                //initialize battle sum and deto sum table with 0
                try
                {
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }

                //initialize other table with 0
                try
                {
                    myCmd = new MySqlCommand(String.Format("" +
                    "INSERT INTO `wows_detonation`.`asia_deto_total_rank` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_btle_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_period_rank` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_total_ratio` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_total_ratio_rank` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_period_ratio` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_period_ratio_rank` (`id`) VALUES ('{0}');",
                    id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to initialize other table!");
                        MessageBox.Show("Fail to initialize other table!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }

                //update battle sum and deto sum for trigger
                try
                {
                    myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_btle_total` SET `{0}`='{1}' WHERE `id`='{2}';", config.Date, btleSum, id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                    myCmd = new MySqlCommand(String.Format("UPDATE `wows_detonation`.`asia_deto_total` SET `{0}`='{1}' WHERE `id`='{2}';", config.Date, detoSum, id + 1), myConn);
                    if (myCmd.ExecuteNonQuery() > 0)
                    {

                    }
                    else
                    {
                        Console.WriteLine("Fail to insert new user deto sum to asia_btle_total!");
                        MessageBox.Show("Fail to insert new user deto sum to asia_player!", "Error!");
                        goto RESTART;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("MySQL execute error!\nDetails:" + e.Message);
                    Console.WriteLine("Retry after 10 seconds...");
                    Thread.Sleep(10000);
                    goto RESTART;
                }

            }

            Console.WriteLine(
                "\nid:\t" + (id + 1) + "\t" +
                "nickname:\t" + username + "\t" +
                "btleSum:\t" + btleSum + "\t" +
                "detoSum:\t" + detoSum + "\t" +
                "nullCount:\t" + nullCount + "\t"
                );
            nullCount = 0;
            Console.WriteLine("");

            id++;

            Console.WriteLine("Done!");
        }
    }
}
