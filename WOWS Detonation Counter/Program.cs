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
using WOWS_Detonation_Counter;

namespace WOWS_Detonation_Counter
{
    class Program
    {
        //execute config
        static Config config;

        static void Main(string[] args)
        {
            MySqlConnection myConn;

            Console.WriteLine("WOWS Detonation Counter");
            Console.WriteLine("by bunnyxt 2018-01-19");
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
                    break;
                case 2:
                    Console.WriteLine("mode2:");
                    Console.WriteLine("rangeMin:\t" + config.Mode2.RangeMin);
                    Console.WriteLine("rangeMax:\t" + config.Mode2.RangeMax);
                    break;
                case 3:
                    Console.WriteLine("mode3:");
                    Console.WriteLine("minId:\t\t" + config.Mode3.MinId);
                    Console.WriteLine("maxId:\t\t" + config.Mode3.MaxId);
                    break;
                case 4:
                    Console.WriteLine("mode4:");
                    Console.WriteLine("targetMin:\t" + config.Mode4.TargetMin);
                    Console.WriteLine("targetMax:\t" + config.Mode4.TargetMax);
                    break;
                case 5:
                    Console.WriteLine("mode5:");
                    Console.WriteLine("targetMinAccountId:\t" + config.Mode5.TargetMinAccountId);
                    Console.WriteLine("targetMinId:\t" + config.Mode5.TargetMinId);
                    Console.WriteLine("targetMaxId:\t" + config.Mode5.TargetMaxId);
                    break;
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
            Console.WriteLine("3 for finding new users id (seperated)");
            Console.WriteLine("4 for adding new users (accompanied with 3)");
            Console.WriteLine("5 for adding new users with id given (accompanied with 3)");
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
                    FindNewTmpIds(myConn);
                    break;
                case 4:
                    AddNewIds(myConn);
                    break;
                case 5:
                    AddNewIdsManual(myConn);
                    break;
                default:
                    Console.WriteLine("Invalid mode id " + config.Mode + " !");
                    Console.WriteWarning("Invalid mode id " + config.Mode + " !");
                    MessageBox.Show("Invalid mode id " + config.Mode + " !", "Error!");
                    throw new Exception();
            }

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

            Console.ReadKey();

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
            //string date = "Y17W44";

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
            while (count < targetSum)
            {
                Console.WriteLine("account_id:" + account_id);

                //initialize data
                detoSum = -1; btleSum = -1;
                username = "";
                isHidden = false;

                //get player personal data
                RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                //check account_id existed or not
                if (playerPersonalDataData.data.playerPersonalDataDataData == null)
                {
                    Console.WriteLine("null  " + (++nullCount));
                    Console.WriteLine("");

                    if (nullCount == 1000)
                    {
                        Console.WriteLine("1000 invalid account_id passed!");
                        MessageBox.Show("1000 invalid account_id passed!", "Notice!");
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

                    //calculate battle sum and deto sum
                    btleSum =
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                            Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                    detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;

                    //insert battle sum and deto sum
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, btleSum), myConn);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                            MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                            goto RESTART;
                        }
                        myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, detoSum), myConn);
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

                //initialize other table with 0
                try
                {
                    myCmd = new MySqlCommand(String.Format("" +
                    "INSERT INTO `wows_detonation`.`asia_deto_ratio_total` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_total` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_ratio_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_rank_total` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_rank_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_deto_period` (`id`) VALUES ('{0}');" +
                    "INSERT INTO `wows_detonation`.`asia_btle_period` (`id`) VALUES ('{0}');",
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
            if (rangeMin < rangeMax && rangeMin >= 1 && rangeMax <= existedMaxId)
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
                //range limit 500000
                int[] ids = new int[500000];
                long[] accountIds = new long[500000];
                string[] usernames = new string[500000];
                bool[] isHiddens = new bool[500000];
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

        }

        public static async void FindNewTmpIds(MySqlConnection myConn)
        {
            //mysql components
            MySqlCommand myCmd;
            MySqlDataReader myRdr;

            //operation instances
            PlayerPersonalData playerPersonalDataData = null;

            int count = 0;
            long account_id = 0;

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY id DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    account_id = myRdr.GetInt64(1);
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
            int minId, maxId;
            Console.WriteLine("Now max account_id is " + account_id);
            Console.WriteLine("Load try id range:");
            minId = config.Mode3.MinId;
            maxId = config.Mode3.MaxId;
            Console.WriteLine(minId);
            Console.WriteLine(maxId);
            if (minId > account_id && minId < maxId)
            {
                Console.WriteLine("Now start getting new ids from " + minId + " to " + maxId + "...");
            }
            else
            {
                Console.WriteLine("Invalid try id range min:" + minId + " max:" + maxId + "!");
                Console.WriteWarning("Invalid try id range min:" + minId + " max:" + maxId + "!");
                MessageBox.Show("Invalid try id range min:" + minId + " max:" + maxId + "!", "Error!");
                throw new Exception();
            }

            //getting new users
            account_id = minId;
            while (account_id <= maxId)
            {
                Console.WriteLine("account_id:" + account_id);

                //get player personal data
                RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                //check account_id existed or not
                if (playerPersonalDataData.data.playerPersonalDataDataData == null)
                {
                    Console.WriteLine("null  " + (++count));
                    Console.WriteLine("");

                    if (count == 1000)
                    {
                        Console.WriteLine("1000 invalid account_id passed!");
                        MessageBox.Show("1000 invalid account_id passed!", "Notice!");
                        break;
                    }
                }
                else
                {
                    //insert id
                    myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player_tmp` (`account_id`) VALUES ('{0}')", account_id), myConn);
                    try
                    {
                        if (myCmd.ExecuteNonQuery() > 0)
                        {
                            Console.WriteLine("added!  " + count);
                            Console.WriteLine("");

                            count = 0;
                        }
                        else
                        {
                            Console.WriteLine("Fail to insert new id to asia_player!");
                            MessageBox.Show("Fail to insert new id to asia_player!", "Error!");
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

                account_id++;
            }
        }

        public static async void AddNewIds(MySqlConnection myConn)
        {
            //mysql components
            MySqlCommand myCmd;
            MySqlDataReader myRdr;
            MySqlConnection myCon;

            //operation instances
            PlayerAchievement playerAchievementData = null;
            PlayerPersonalData playerPersonalDataData = null;

            //sql manage data
            //string date = "Y17W44";

            int id, existedMaxId = 0, detoSum = -1, btleSum = -1;
            long account_id = 0, existedMaxAccountId = 0, existedMinAccountId = 0;
            string username = "";
            bool isHidden = false;

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY `id` DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxId = myRdr.GetInt32(0);
                }
                myRdr.Close();
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp ORDER BY `account_id` LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMinAccountId = myRdr.GetInt64(0);
                }
                myRdr.Close();
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp ORDER BY `account_id` DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxAccountId = myRdr.GetInt64(0);
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
            int targetMin, targetMax;
            Console.WriteLine("Now min account_id is " + existedMinAccountId);
            Console.WriteLine("Now max account_id is " + existedMaxAccountId);
            Console.WriteLine("Load target new account_id range:");
            targetMin = config.Mode4.TargetMin;
            targetMax = config.Mode4.TargetMax;
            Console.WriteLine(targetMin);
            Console.WriteLine(targetMax);
            if (targetMin >= existedMinAccountId && targetMin < targetMax && targetMax <= existedMaxAccountId)
            {
                Console.WriteLine("Now start getting " + targetMin + " ~ " + targetMax + " new ids...");
            }
            else
            {
                Console.WriteLine("Invalid target new account_id range min:" + targetMin + " max:" + targetMax + " !");
                Console.WriteWarning("Invalid target new account_id range min:" + targetMin + " max:" + targetMax + " !");
                MessageBox.Show("Invalid target new account_id range min:" + targetMin + " max:" + targetMax + " !", "Error!");
                throw new Exception();
            }

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp WHERE `account_id` >= " + targetMin + " && `account_id` <= " + targetMax + " ORDER BY `account_id`;", myConn);
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

                //getting new ids
                id = existedMaxId;
                while (myRdr.Read())
                {
                    //get account_id
                    account_id = myRdr.GetInt64(0);

                    Console.WriteLine("account_id:" + account_id);

                    //initialize data
                    detoSum = -1; btleSum = -1;
                    username = "";
                    isHidden = false;

                    //get player personal data
                    RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                    //check account_id existed or not
                    if (playerPersonalDataData.data.playerPersonalDataDataData == null)
                    {
                        Console.WriteLine("null");
                        Console.WriteLine("");
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '1')", id + 1, account_id, username), myCon);
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                                MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                                goto RESTART;
                            }
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myCon);
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '0')", id + 1, account_id, username), myCon);
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

                        //calculate battle sum and deto sum
                        btleSum =
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                        detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;

                        //insert battle sum and deto sum
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, btleSum), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                                MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                                goto RESTART;
                            }
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, detoSum), myCon);
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

                    //initialize other table with 0
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_rank_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_rank_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_btle_period` (`id`) VALUES ('{0}');",
                        id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1), myCon);
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

                    //remove account_id from aisa_player_tmp
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("DELETE FROM `wows_detonation`.`asia_player_tmp` WHERE `account_id`='{0}';", account_id), myCon);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to delete row!");
                            MessageBox.Show("Fail to delete row!", "Error!");
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

                    Console.WriteLine(
                        "id:\t" + (id + 1) + "\t" +
                        "nickname:\t" + username + "\t" +
                        "btleSum:\t" + btleSum + "\t" +
                        "detoSum:\t" + detoSum + "\t"
                        );
                    Console.WriteLine("");

                    id++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

        }

        public static async void AddNewIdsManual(MySqlConnection myConn)
        {
            //mysql components
            MySqlCommand myCmd;
            MySqlDataReader myRdr;
            MySqlConnection myCon;

            //operation instances
            PlayerAchievement playerAchievementData = null;
            PlayerPersonalData playerPersonalDataData = null;

            //sql manage data
            //string date = "Y17W44";

            int id, existedMaxId = 0, detoSum = -1, btleSum = -1;
            long account_id = 0, existedMaxAccountId = 0, existedMinAccountId = 0;
            string username = "";
            bool isHidden = false;

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player ORDER BY `id` DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxId = myRdr.GetInt32(0);
                }
                myRdr.Close();
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp ORDER BY `account_id` LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMinAccountId = myRdr.GetInt64(0);
                }
                myRdr.Close();
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp ORDER BY `account_id` DESC LIMIT 1;", myConn);
                myRdr = myCmd.ExecuteReader();
                while (myRdr.Read())
                {
                    existedMaxAccountId = myRdr.GetInt64(0);
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
            int targetMinAccountId, targetMinId, targetMaxId;
            Console.WriteLine("Now min account_id is " + existedMinAccountId);
            Console.WriteLine("Now max account_id is " + existedMaxAccountId);
            Console.WriteLine("Now max id is " + existedMaxId);
            Console.WriteLine("Load target new account_id start:");
            targetMinAccountId = config.Mode5.TargetMinAccountId;
            Console.WriteLine(targetMinAccountId);
            Console.WriteLine("Load target new id range:");
            targetMinId = config.Mode5.TargetMinId;
            targetMaxId = config.Mode5.TargetMaxId;
            Console.WriteLine(targetMinId);
            Console.WriteLine(targetMaxId);
            if (targetMinAccountId >= existedMinAccountId && targetMinAccountId < existedMaxAccountId && targetMaxId >= targetMinId)
            {
                Console.WriteLine("Now start getting new id" + targetMinId + " ~ " + targetMaxId + "from account_id " + targetMinAccountId + "...");
            }
            else
            {
                Console.WriteLine("Invalid targert new account_id start: " + targetMinAccountId + " or new id range min:" + targetMinId + " max:" + targetMaxId + "!");
                Console.WriteWarning("Invalid targert new account_id start: " + targetMinAccountId + " or new id range min:" + targetMinId + " max:" + targetMaxId + "!");
                MessageBox.Show("Invalid targert new account_id start: " + targetMinAccountId + " or new id range min:" + targetMinId + " max:" + targetMaxId + "!", "Error!");
                throw new Exception();
            }

            try
            {
                myCmd = new MySqlCommand("SELECT * FROM wows_detonation.asia_player_tmp WHERE `account_id` >= " + targetMinAccountId + " ORDER BY `account_id` LIMIT " + (targetMaxId - targetMinId + 1) + ";", myConn);
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

                //getting new ids
                int i = 0;
                long[] accountIdArray = new long[100000];
                while (myRdr.Read())
                {
                    accountIdArray[i] = myRdr.GetInt64(0);
                    i++;
                }
                myRdr.Close();

                //main for loop
                id = targetMinId - 1;
                for (i = 0; i < (targetMaxId - targetMinId + 1); i++)
                {
                    //get account_id
                    account_id = accountIdArray[i];

                    Console.WriteLine("account_id:" + account_id);

                    //initialize data
                    detoSum = -1; btleSum = -1;
                    username = "";
                    isHidden = false;

                    //get player personal data
                    RESTART: playerPersonalDataData = await Proxy.GetPlayerPersonalDataAsync(account_id);

                    //check account_id existed or not
                    if (playerPersonalDataData.data.playerPersonalDataDataData == null)
                    {
                        Console.WriteLine("null");
                        Console.WriteLine("");
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '1')", id + 1, account_id, username), myCon);
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`) VALUES ('{0}');", id + 1), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                                MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                                goto RESTART;
                            }
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`) VALUES ('{0}');", id + 1), myCon);
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
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_player` (`id`, `account_id`, `user_name`, `is_hidden`) VALUES ('{0}', '{1}', '{2}', '0')", id + 1, account_id, username), myCon);
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

                        //calculate battle sum and deto sum
                        btleSum =
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.club.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.pvp.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_solo.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div2.battles) +
                                Convert.ToInt32(playerPersonalDataData.data.playerPersonalDataDataData.statistics.rank_div3.battles);
                        detoSum = playerAchievementData.data.playerAchievementDataData.battle.DETONATED;

                        //insert battle sum and deto sum
                        try
                        {
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_btle_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, btleSum), myCon);
                            if (myCmd.ExecuteNonQuery() > 0)
                            {

                            }
                            else
                            {
                                Console.WriteLine("Fail to insert new user battle sum to asia_btle_total!");
                                MessageBox.Show("Fail to insert new user battle sum to asia_player!", "Error!");
                                goto RESTART;
                            }
                            myCmd = new MySqlCommand(String.Format("INSERT INTO `wows_detonation`.`asia_deto_total` (`id`, `{0}`) VALUES ('{1}', '{2}');", config.Date, id + 1, detoSum), myCon);
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

                    //initialize other table with 0
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_rank_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_ratio_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_rank_total` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_rank_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_deto_period` (`id`) VALUES ('{0}');" +
                        "INSERT INTO `wows_detonation`.`asia_btle_period` (`id`) VALUES ('{0}');",
                        id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1, id + 1), myCon);
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

                    //remove account_id from aisa_player_tmp
                    try
                    {
                        myCmd = new MySqlCommand(String.Format("DELETE FROM `wows_detonation`.`asia_player_tmp` WHERE `account_id`='{0}';", account_id), myCon);
                        if (myCmd.ExecuteNonQuery() > 0)
                        {

                        }
                        else
                        {
                            Console.WriteLine("Fail to delete row!");
                            MessageBox.Show("Fail to delete row!", "Error!");
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

                    Console.WriteLine(
                        "id:\t" + (id + 1) + "\t" +
                        "nickname:\t" + username + "\t" +
                        "btleSum:\t" + btleSum + "\t" +
                        "detoSum:\t" + detoSum + "\t"
                        );
                    Console.WriteLine("");

                    id++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                MessageBox.Show("MySQL execution(s) ran into an error!\nDetails:" + e.Message, "Error!");
                throw;
            }

        }
    }
}
