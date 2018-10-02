using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WOWS_Detonation_Counter
{
    public class Config
    {
        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("mySQLDatabase")]
        public MySqlDatabase MySqlDatabase { get; set; }

        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("mode1")]
        public Mode1 Mode1 { get; set; }

        [JsonProperty("mode2")]
        public Mode2 Mode2 { get; set; }

        [JsonProperty("mode3")]
        public Mode3 Mode3 { get; set; }

        [JsonProperty("mode4")]
        public Mode4 Mode4 { get; set; }

        [JsonProperty("mode5")]
        public Mode5 Mode5 { get; set; }

        [JsonProperty("mode6")]
        public Mode6 Mode6 { get; set; }

        [JsonProperty("mode7")]
        public Mode7 Mode7 { get; set; }

        [JsonProperty("mode8")]
        public Mode8 Mode8 { get; set; }

        [JsonProperty("mode998")]
        public Mode998 Mode998 { get; set; }

        [JsonProperty("mail")]
        public Mail Mail { get; set; }

        public static Config FromJson(string json) => JsonConvert.DeserializeObject<Config>(json, Converter.Settings);
    }

    public class Mode1
    {
        [JsonProperty("targetSum")]
        public int TargetSum { get; set; }

        [JsonProperty("startAccountId")]
        public long StartAccountId { get; set; }
    }

    public class Mode2
    {
        [JsonProperty("rangeMin")]
        public int RangeMin { get; set; }

        [JsonProperty("rangeMax")]
        public int RangeMax { get; set; }
    }

    public class Mode3
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }
    }

    public class Mode4
    {
        [JsonProperty("accountId")]
        public List<long> AccountId { get; set; }
    }

    public class Mode5
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }
    }

    public class Mode6
    {
        [JsonProperty("id")]
        public List<int> Id { get; set; }
    }

    public class Mode7
    {
        [JsonProperty("fileName")]
        public string FileName { get; set; }
    }

    public class Mode8
    {
        [JsonProperty("id")]
        public List<int> Id { get; set; }
    }

    public class Mode998
    {
        [JsonProperty("accountId")]
        public List<long> AccountId { get; set; }
    }

    public class MySqlDatabase
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("database")]
        public string Database { get; set; }
    }

    public class Mail
    {
        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("receiver")]
        public string Receiver { get; set; }

        [JsonProperty("clientHost")]
        public string ClientHost { get; set; }

        [JsonProperty("creditCode")]
        public string CreditCode { get; set; }
    }

    public static class Serialize
    {
        public static string ToJson(this Config self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
