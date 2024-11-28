using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ThrowException.CSharpLibs.LogLib;

namespace Speculatores
{
    public class AmtsblattEntry
    {
        public Guid Id { get; private set; }

        public DateTime Date { get; private set; }

        public string Office { get; private set; }

        public string Title { get; private set; }

        public string Text { get; private set; }

        public IEnumerable<string> Urls { get; private set; }

        public AmtsblattEntry(Guid id, DateTime date, string office, string title, string text, IEnumerable<string> urls)
        {
            Id = id;
            Date = date;
            Office = office;
            Title = title;
            Text = text;
            Urls = urls;
        }
    }

    public class AmtsblattReader
    {
        private readonly ILogger _logger;

        public AmtsblattReader(ILogger logger)
        {
            _logger = logger;
        }

        private string GetElementValue(XElement baseElement, string elementName)
        {
            var value = baseElement.Elements(elementName).SingleOrDefault()?.Value;
            if (value == string.Empty) return null;
            return value;
        }

        private string CleanUp(string text)
        {
            var current = text
                .Replace("\n", " ")
                .Replace("\t", " ");
            var old = string.Empty;
            while (current != old)
            {
                old = current;
                current = Regex.Replace(current, @"<\/?[a-zA-Z0-9]+\/?>", " ", RegexOptions.Multiline);
            }
            old = string.Empty;
            while (current != old)
            {
                old = current;
                current = Regex.Replace(current, @"\s{2,}", " ", RegexOptions.Multiline);
            }
            return current;
        }

        public IEnumerable<AmtsblattEntry> Get(int maxResults, TimeSpan maxAge, string tenant = null, string rubric = null, string subRubric = null, Func<Guid, bool> load = null)
        {
            var listUrl = "https://amtsblattportal.ch/api/v1/publications/xml?publicationStates=PUBLISHED";
            if (!string.IsNullOrEmpty(tenant))
            {
                listUrl += "&tenant=" + tenant;
            }
            if (!string.IsNullOrEmpty(rubric))
            {
                listUrl += "&rubrics=" + rubric;
            }
            if (!string.IsNullOrEmpty(subRubric))
            {
                listUrl += "&subRubrics=" + subRubric;
            }
            var list = DownloadXml(listUrl);
            var publications = list.Root.Elements("publication");
            _logger.Debug("Amtsblatt download list has {0} entries", publications.Count());
            int pubsReturned = 0;
            int pubsOverage = 0;
            int pubsNotLoaded = 0;
            int pubsPending = 0;
            foreach (var publication in publications)
            {
                var meta = publication.Element("meta");
                var dateString = meta.Element("publicationDate").Value;
                var date = DateTime.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (DateTime.Now.Subtract(date) < maxAge)
                {
                    var idString = meta.Element("id").Value;
                    var id = Guid.Parse(idString);
                    if ((load == null) || load(id))
                    {
                        if (pubsReturned < maxResults)
                        {
                            var office = meta.Element("registrationOffice").Element("displayName").Value;
                            var refUrl = publication.Attribute("ref").Value;
                            var item = DownloadXml(refUrl);
                            var content = item.Root.Element("content");

                            var title =
                                GetElementValue(content, "enactmentTitle") ??
                                GetElementValue(content, "title") ??
                                GetElementValue(content, "titleOfVote") ??
                                GetElementValue(content, "titleOfPolling") ??
                                string.Empty;
                            var text =
                                GetElementValue(content, "enactment") ??
                                GetElementValue(content, "publication") ??
                                string.Empty;
                            var addon =
                                GetElementValue(content, "fullResolution") ??
                                string.Empty;
                            text = string.Join(" ", (new string[] { text, addon }).Where(x => !string.IsNullOrEmpty(x)));
                            text = CleanUp(text);

                            var fileUrls = new List<string>();
                            foreach (var fileId in item.Root
                                .Elements("attachments")?.SelectMany(e =>
                                e?.Elements("fileId").Select(x => x?.Value))
                                .Where(i => !string.IsNullOrEmpty(i)))
                            {
                                if (!string.IsNullOrEmpty(fileId))
                                {
                                    fileUrls.Add(string.Format("https://amtsblatt.zg.ch/api/v1/publications/{0}/attachments/{1}", idString, fileId));
                                }
                            }
                            pubsReturned++;
                            yield return new AmtsblattEntry(id, date, office, title, text, fileUrls);
                        }
                        else
                        {
                            pubsPending++;
                        }
                    }
                    else
                    {
                        pubsNotLoaded++;
                    }
                }
                else
                {
                    pubsOverage++;
                }
            }
            _logger.Debug("Amtsblatt publications overage {0} not loaded {1} pending {2} returned {3}", pubsOverage, pubsNotLoaded, pubsPending, pubsReturned);
        }

        private XDocument DownloadXml(string url)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(url);

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);

            return XDocument.Parse(responseText);
        }
    }
}
