using fs24bot3.Core;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using fs24bot3.Helpers;

namespace fs24bot3.Core;

public class HttpTools
{
    readonly CookieContainer Cookies = new CookieContainer();
    public readonly HttpClient Client = new HttpClient();

    public HttpTools()
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");
    }

    public string RecursiveHtmlDecode(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        var tmp = HttpUtility.HtmlDecode(str);
        while (tmp != str)
        {
            str = tmp;
            tmp = HttpUtility.HtmlDecode(str);
        }
        return str; //completely decoded string
    }

    public IPEndPoint ParseHostname(string host)
    {
        var hostname = host.Split(":");
        var port = 25565;
        if (hostname.Length > 1) { _ = int.TryParse(hostname[1], out port); }
        if (IPAddress.TryParse(hostname[0], out IPAddress ip))
        {
            Log.Verbose("parsed addr: {0}:{1}", ip, port);
            return new IPEndPoint(ip, port);
        }
        else
        {
            return new IPEndPoint(Dns.GetHostEntry(hostname[0]).AddressList[0], port);
        }
    }

    public async Task<string> PostJson(string url, object jsonObj)
    {
        HttpContent c = new StringContent(JsonConvert.SerializeObject(jsonObj), Encoding.UTF8, "application/json");

        var response = await Client.PostAsync(url, c);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        return responseString;
    }
    
    public async Task<T> GetJson<T>(string url)
    {
        var response = await GetResponseAsync(url);
        response.EnsureSuccessStatusCode();
        
        return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync(), 
            JsonSerializerHelper.OPTIMIMAL_SETTINGS);
    }

    public async Task<string> MakeRequestAsyncNoCookie(string url)
    {
        using var handler = new HttpClientHandler();
        try
        {
            var result = await Client.GetAsync(url);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }
            throw new InvalidDataException($"{result.StatusCode}: {result.Content.ReadAsStringAsync().Result}");
        }
        catch (Exception e)
        {
            Log.Warning("Request to address {0} failed: {1}", url, e.Message);
            return null;
        }
    }

    public async Task<HttpResponseMessage> GetResponseAsync(string url)
    {
        using var handler = new HttpClientHandler { CookieContainer = Cookies };
        try
        {
            var result = await Client.GetAsync(url);
            result.EnsureSuccessStatusCode();
            return result;
        }
        catch (Exception e)
        {
            Log.Warning("Request to address {0} failed: {1}", url, e.Message);
            return null;
        }
    }
    public async Task<bool> PingHost(string nameOrAddress)
    {
        bool pingable = false;
        Ping pinger = null;

        try
        {
            pinger = new Ping();
            PingReply reply = await pinger.SendPingAsync(nameOrAddress);
            pingable = reply.Status == IPStatus.Success;
        }
        catch (PingException e)
        {
            Log.Warning("Ping failed to address: {0} exception: {1}", nameOrAddress, e.Message);
            return false;
        }
        finally
        {
            if (pinger != null)
            {
                pinger.Dispose();
            }
        }

        return pingable;
    }

    public async Task DownloadFile(string filename, string url)
    {
        var resp = await GetResponseAsync(url);
        await File.WriteAllBytesAsync(filename, await resp.Content.ReadAsByteArrayAsync());
    }
    public async Task<string> GetTextPlainResponse(string rawurl)
    {
        var response = await GetResponseAsync(rawurl);
        if (response != null)
        {
            if (response.Content.Headers.ContentType.MediaType == "text/plain")
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                throw new InvalidDataException($"Ошибка в Content-Type запроса: Необходимый Content-Type: text/plain получилось: {response.Content.Headers.ContentType.MediaType}");
            }
        }
        return null;
    }
}