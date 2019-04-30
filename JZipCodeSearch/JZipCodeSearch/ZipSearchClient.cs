using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RKSoftware.JZipCodeSearch
{
    public static class ZipSearchClient
    {
        internal static string _urlBase { get; } = "https://www.post.japanpost.jp/cgi-zip/";
        internal static string _urlZipSearch { get; } = $"{_urlBase}zipcode.php";
        internal static string _urlAddressSearch { get; } = $"{_urlZipSearch}?zip={{0}}";

        static HttpClient _httpClient { get; set; }
        internal static HttpClient HttpClient { get => _httpClient ?? (_httpClient = new HttpClient()); }

        static Regex _regDataRows { get; } = new Regex("<tr>(.|\\n)*? class=\"data\"(.|\\n)*?</tr>");
        internal static string[] GetDataRows(string html) => _regDataRows.Matches(html)?.OfType<Match>().Select(m => m.Value).ToArray();


        public static void Init(HttpClient httpClient) => _httpClient = httpClient;
        public static async Task<Address[]> ZipToAddress(string code) => await JZipCodeSearch.ZipToAddress.Search(code);
        public static async Task<Address[]> AddressToZip(string address) => await JZipCodeSearch.AddressToZip.Search(address);
    }

    internal static class ZipToAddress
    {
        public static async Task<Address[]> Search(string code)
        {
            var searchurl = string.Format(ZipSearchClient._urlAddressSearch, System.Net.WebUtility.UrlEncode(code));
            var html = await ZipSearchClient.HttpClient.GetStringAsync(searchurl);
            var addressList = GetAddressList(html).ToArray();
            return addressList;
        }

        static IEnumerable<Address> GetAddressList(string html)
        {
            var addressHtmlList = ZipSearchClient.GetDataRows(html);
            foreach (var addressHtml in addressHtmlList)
                yield return GetAddress(addressHtml);
        }

        static Address GetAddress(string html)
        {
            var values = GetDataElements(html).Select(elm => GetText(elm)).ToArray();
            var kana = GetKana(html);
            var address = new Address(values, kana);
            return address;
        }

        static string GetKana(string html)
        {
            var elm = GetKanaElement(html);
            var kana = GetText(elm);
            return kana;
        }

        static Regex _regDataElements { get; } = new Regex(" class=\"data\"(.|\\n)*?>[^>\\s]+<");
        static string[] GetDataElements(string html) => _regDataElements.Matches(html)?.OfType<Match>().Select(m => m.Value).ToArray();

        static Regex _regGetText { get; } = new Regex(">[^>\\s]+<");
        static string GetText(string html) => _regGetText.Match(html)?.Value.Replace("<", "").Replace(">", "").Replace("&nbsp;", "");

        static Regex _regKanaElement { get; } = new Regex(" class=\"comment\"(.|\\n)*?>[^>\\s]+<");
        static string GetKanaElement(string html) => _regKanaElement.Match(html)?.Value;
    }

    internal static class AddressToZip
    {

        public static async Task<Address[]> Search(string address)
        {
            var parameters = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"addr", address}
            });
            var responce = await ZipSearchClient.HttpClient.PostAsync(ZipSearchClient._urlZipSearch, parameters);
            var html = await responce.Content.ReadAsStringAsync();
            var zipList = await GetZipList(html);
            return zipList;
        }

        static async Task<Address[]> GetZipList(string html)
        {
            var hrefs = ZipSearchClient.GetDataRows(html).Select(data => GetHref(data)).ToArray();
            var addresss = await Task.WhenAll(hrefs.Select(async href => await GetZipAndAddress(href)).AsParallel());
            return addresss;
        }

        static async Task<Address> GetZipAndAddress(string href)
        {
            var url = $"{ZipSearchClient._urlBase}{href}";
            var html = await ZipSearchClient.HttpClient.GetStringAsync(url);
            var zip = GetZip(html);
            var address = GetAddresss(zip, html);
            return address;
        }

        static Regex _regZip { get; } = new Regex("<span class=\"zip-code\">(.|\\n)*?</span>");
        static string GetZip(string html) => GetText2(_regZip.Match(html)?.Value.Replace("〒", ""));

        static Regex _regGetText2 { get; } = new Regex(">([^>]|\\n)*?</");
        static string GetText2(string html) => _regGetText2.Match(html)?.Value.Replace("</", "").Replace(">", "").Trim();

        static Regex _regAddress { get; } = new Regex(" class=\"data\" (.|\\n)*?</div>");
        static Address GetAddresss(string zip, string html)
        {
            var addressArea = _regAddress.Match(html)?.Value;
            var valuesElement = _regGetText2.Matches(addressArea).OfType<Match>().Select(m => m.Value).ToArray();
            var values = valuesElement.Select(m => GetText2(m)).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

            return new Address(zip, values);
        }
        static Regex _regHref { get; } = new Regex(" href=\".+?\"");
        static string GetHref(string html) => _regHref.Match(html)?.Value.Replace(" href=", "").Replace("\"", "");
    }

    public struct Address
    {
        public string ZipCode { get; }
        public string Prefecture { get; }
        public string City { get; }
        public string Machi { get; }
        public string Kana { get; }
        public Address(string[] values)
        {
            ZipCode = values?.FirstOrDefault();
            Prefecture = values?.Skip(1).FirstOrDefault();
            City = values?.Skip(2).FirstOrDefault();
            Machi = values?.Skip(3).FirstOrDefault();
            Kana = values?.Skip(4).FirstOrDefault();
        }
        public Address(string zip, string[] values) : this(new string[] { zip }.Union(values).ToArray()) {; }
        public Address(string[] values, string kana) : this(values.Take(4).Union(new[] { kana }).ToArray()) {; }

        public override string ToString()
        {
            return $"{ZipCode} {Prefecture} {City} {Machi} {Kana}";
        }
    }

}
