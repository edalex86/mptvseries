using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using WindowPlugins.GUITVSeries.TmdbAPI.DataStructures;
using WindowPlugins.GUITVSeries.TmdbAPI.Extensions;
using System.Web.Script.Serialization;
namespace tvdbapiv4
{
    class APIv4
    {
        #region Web Events

        // these events can be used to log data sent / received from fanart.tv
        public delegate void OnDataSendDelegate(string url, string postData);
        public delegate void OnDataReceivedDelegate(string response, HttpWebResponse webResponse);
        public delegate void OnDataErrorDelegate(string error);

        public static event OnDataSendDelegate OnDataSend;
        public static event OnDataReceivedDelegate OnDataReceived;
        public static event OnDataErrorDelegate OnDataError;

        #endregion
        #region Settings

        // these settings should be set before sending data to tmdb        
        public static string UserAgent { get; set; }

        #endregion
        //----------------------
        public static JavaScriptSerializer js = new JavaScriptSerializer();
        static string GetFromTvdbv4(string address, string post = null, int delayRequest = 0)
        {
            if (delayRequest > 0)
                Thread.Sleep(1000 + delayRequest);

            OnDataSend?.Invoke(address, null);

            var headerCollection = new WebHeaderCollection();

            var request = WebRequest.Create(address) as HttpWebRequest;

            request.KeepAlive = true;
            request.Method = (post != null ? "POST" : "GET");
            request.ContentLength = 0;
            request.Timeout = 120000;
            request.ContentType = "application/json";
            request.UserAgent = UserAgent;
            if (post != null)
            {
                request.Method = "POST";
                byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(post);
                request.ContentLength = byteArray.Length;
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }
            }

            string strResponse = null;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response == null) return null;

                Stream stream = response.GetResponseStream();

                StreamReader reader = new StreamReader(stream);
                strResponse = reader.ReadToEnd();

                headerCollection = response.Headers;

                OnDataReceived?.Invoke(strResponse, response);

                stream.Close();
                reader.Close();
                response.Close();
            }
            catch (WebException wex)
            {
                string errorMessage = wex.Message;
                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = wex.Response as HttpWebResponse;

                    string headers = string.Empty;
                    foreach (string key in response.Headers.AllKeys)
                    {
                        headers += string.Format("{0}: {1}, ", key, response.Headers[key]);
                    }
                    errorMessage = string.Format("Protocol Error, Code = '{0}', Description = '{1}', Url = '{2}', Headers = '{3}'", (int)response.StatusCode, response.StatusDescription, address, headers.TrimEnd(new char[] { ',', ' ' }));

                    if ((int)response.StatusCode == 429)
                    {
                        int retry = 0;
                        int.TryParse(response.Headers["Retry-After"], out retry);

                        errorMessage = string.Format("Request Rate Limiting is in effect, retrying request in {0} seconds. Url = '{1}'", retry, address);

                        OnDataError?.Invoke(errorMessage);

                        return GetFromTvdbv4(address, post, retry * 1000);
                    }
                }

                OnDataError?.Invoke(errorMessage);

                strResponse = null;
            }
            catch (IOException ioe)
            {
                string errorMessage = string.Format("Request failed due to an IO error, Description = '{0}', Url = '{1}', Method = 'GET'", ioe.Message, address);

                OnDataError?.Invoke(ioe.Message);

                strResponse = null;
            }

            return strResponse;
        }

        public partial class swaggerClient
        {
            private string _baseUrl = "https://api4.thetvdb.com/v4";
            private string apikey = "c3900f5a-0213-438b-ab45-dc4f4756cfc0";

            public string BaseUrl
            {
                get { return _baseUrl; }
                set { _baseUrl = value; }
            }

            /// <returns>response</returns>
            /// <summary>create an auth token. The token has one month valition length.</summary>
            public LoginResponse Login(Body body)
            {
                string postdata = js.Serialize(body);
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/login"), postdata);
                return js.Deserialize<LoginResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="id">id</param>
            /// <summary>Returns a single artwork base record</summary>
            public ArtworkBaseResponse GetArtworkBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/artwork/{id}", id)));
                return js.Deserialize<ArtworkBaseResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="id">id</param>
            /// <summary>Returns a single artwork extended record</summary>
            public ArtworkExtendedResponse GetArtworkExtended(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/artwork/{id}/extended", id)));
                return js.Deserialize<ArtworkExtendedResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns list of artwork status records</summary>
            public ArtworkStatusListResponse GetAllArtworkStatuses()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/artwork/statuses"));
                return js.Deserialize<ArtworkStatusListResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns a list of artworkType records</summary>
            public ArtworkTypeListResponse GetAllArtworkTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/artwork/types"));
                return js.Deserialize<ArtworkTypeListResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns a list of award base records</summary>
            public AwardBaseListResponse GetAllAwards()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/awards"));
                return js.Deserialize<AwardBaseListResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="id">id</param>
            /// <summary>Returns a single award base record</summary>
            public AwardBaseResponse GetAward(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/awards/{id}", id)));
                return js.Deserialize<AwardBaseResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="id">id</param>
            /// <summary>Returns a single award extended record</summary>
            public AwardExtendedResponse GetAwardExtended(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/awards/{id}/extended", id)));
                return js.Deserialize<AwardExtendedResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns a single award category base record</summary>
            public AwardCategoryBaseResponse GetAwardCategory(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/awards/categories/{id}", id)));
                return js.Deserialize<AwardCategoryBaseResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns a single award category extended record</summary>
            public AwardCategoryExtendedResponse GetAwardCategoryExtended(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/awards/categories/{id}/extended", id)));
                return js.Deserialize<AwardCategoryExtendedResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns character base record</summary>
            public CharacterResponse GetCharacterBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/characters/{id}", id)));
                return js.Deserialize<CharacterResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="page">name</param>
            /// <summary>Returns a paginated list of company records</summary>
            public CompanyListResponse GetAllCompanies(double? page)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/companies?", (page != null ? string.Format("page={page}", page) : "")));
                return js.Deserialize<CompanyListResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns all company type records</summary>
            public CompanyTypeListResponse GetCompanyTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/companies/types"));
                return js.Deserialize<CompanyTypeListResponse>(response);
            }

            /// <returns>response</returns>
            /// <param name="id">id</param>
            /// <summary>Returns a company record</summary>
            public CompanyResponse GetCompany(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/companies/{id}", id)));
                return js.Deserialize<CompanyResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list content rating records</summary>
            public ContentRatingResponse GetAllContentRatings()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/content/ratings"));
                return js.Deserialize<ContentRatingResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of country records</summary>
            public CountryResponse GetAllCountries()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/countries"));
                return js.Deserialize<CountryResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns the active entity types</summary>
            public EntityTypeResponse GetEntityTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/entities/types"));
                return js.Deserialize<EntityTypeResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns episode base record</summary>
            public EpisodeBaseResponse GetEpisodeBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/episodes/{id}", id)));
                return js.Deserialize<EpisodeBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns episode extended record</summary>
            public EpisodeExtendedResponse GetEpisodeExtended(double id, Meta? meta)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/episodes/{id}/extended?", id, (meta != null ? string.Format("meta={meta}", meta) : ""))));
                return js.Deserialize<EpisodeExtendedResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns episode translation record</summary>
            public TranslationResponse GetEpisodeTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/episodes/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of gender records</summary>
            public GenderListResponse GetAllGenders()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/genders"));
                return js.Deserialize<GenderListResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of genre records</summary>
            public GenderBaseListResponse GetAllGenres()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/genres"));
                return js.Deserialize<GenderBaseListResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns genre record</summary>
            public GenreBaseResponse GetGenreBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/genres/{id}", id)));
                return js.Deserialize<GenreBaseResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of inspiration types records</summary>
            public InspirationTypeResponse GetAllInspirationTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/inspiration/types"));
                return js.Deserialize<InspirationTypeResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of language records</summary>
            public LanguageListResponse GetAllLanguages()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/languages"));
                return js.Deserialize<LanguageListResponse>(response);
            }

            /// <param name="page">page number</param>
            /// <returns>response</returns>
            /// <summary>Returns list of list base records</summary>
            public ListBaseListResponse GetAllLists(double? page)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/lists?", (page != null ? string.Format("page={page}", page) : ""))));
                return js.Deserialize<ListBaseListResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>returns an list base record</summary>
            public ListBaseResponse GetList(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/lists/{id}", id)));
                return js.Deserialize<ListBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>returns a list extended record</summary>
            public ListExtendedResponse GetListExtended(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/lists/{id}/extended", id)));
                return js.Deserialize<ListExtendedResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns list translation record</summary>
            public TranslationResponse GetListTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/lists/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <param name="page">page number</param>
            /// <returns>response</returns>
            /// <summary>returns list of movie base records</summary>
            public MovieBaseListResponse GetAllMovie(double? page)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/movies?", (page != null ? string.Format("page={page}", page) : ""))));
                return js.Deserialize<MovieBaseListResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns movie base record</summary>
            public MovieBaseResponse GetMovieBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/movies/{id}", id)));
                return js.Deserialize<MovieBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="meta">meta</param>
            /// <returns>response</returns>
            /// <summary>Returns movie extended record</summary>
            public MovieExtendedResponse GetMovieExtended(double id, Meta? meta)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/movies/{id}/extended?", id), (meta != null ? string.Format("meta={meta}", meta) : "")));
                return js.Deserialize<MovieExtendedResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns movie translation record</summary>
            public TranslationResponse GetMovieTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/movies/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of status records</summary>
            public StatusResponse GetAllMovieStatuses()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/movies/statuses"));
                return js.Deserialize<StatusResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns people base record</summary>
            public PeopleBaseResponse GetPeopleBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/people/{id}", id)));
                return js.Deserialize<PeopleBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns people extended record</summary>
            public PeopleExtendedResponse GetPeopleExtended(double id, Meta? meta)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/people/{id}/extended?", id), (meta != null ? string.Format("meta={meta}", meta) : "")));
                return js.Deserialize<PeopleExtendedResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns people translation record</summary>
            public TranslationResponse GetPeopleTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/people/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of peopleType records</summary>
            public PeopleTypeResponse GetAllPeopleTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/people/types"));
                return js.Deserialize<PeopleTypeResponse>(response);
            }

            /// <param name="q">additional search query param</param>
            /// <param name="query">additional search query param</param>
            /// <param name="type">restrict results to entity type movie|series|person|company</param>
            /// <param name="remote_id">restrict results to remote id</param>
            /// <param name="year">restrict results to a year for movie|series</param>
            /// <param name="offset">offset results</param>
            /// <param name="limit">limit results</param>
            /// <returns>response</returns>
            /// <summary>Returns a search result record</summary>
            public SearchResultResponse GetSearchResults(string q, string query, string type, string remote_id, double? year, double? offset, double? limit)
            {
                string request = string.Concat(BaseUrl, "/search?");
                if (q != null)
                {
                    request += string.Concat("&q=", q);
                }
                if (query != null)
                {
                    request += string.Concat("&query=", query);
                }
                if (type != null)
                {
                    request += string.Concat("&type=", type);
                }
                if (remote_id != null)
                {
                    request += string.Concat("&remote_id=", remote_id);
                }
                if (year != null)
                {
                    request += string.Concat("&year=", year);
                }
                if (offset != null)
                {
                    request += string.Concat("&offset=", offset);
                }
                if (limit != null)
                {
                    request += string.Concat("&limit=", limit);
                }
                string response = GetFromTvdbv4(request);
                return js.Deserialize<SearchResultResponse>(response);
            }

            /// <param name="page">page number</param>
            /// <returns>response</returns>
            /// <summary>returns list of seasons base records</summary>
            public SeasonBaseListResponse GetAllSeasons(double? page)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/seasons?", (page != null ? string.Format("page={page}", page) : ""))));
                return js.Deserialize<SeasonBaseListResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns season base record</summary>
            public SeasonBaseResponse GetSeasonBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/seasons/{id}", id)));
                return js.Deserialize<SeasonBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns season extended record</summary>
            public SeasonExtendedResponse GetSeasonExtended(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/seasons/{id}/extended", id)));
                return js.Deserialize<SeasonExtendedResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>Returns season type records</summary>
            public SeasonTypeResponse GetSeasonTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/seasons/types"));
                return js.Deserialize<SeasonTypeResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns season translation record</summary>
            public TranslationResponse GetSeasonTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/seasons/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <param name="page">page number</param>
            /// <returns>response</returns>
            /// <summary>returns list of series base records</summary>
            public SeriesBaseListResponse GetAllSeries(double? page)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series?", (page != null ? string.Format("page={page}", page) : ""))));
                return js.Deserialize<SeriesBaseListResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns series base record</summary>
            public SeriesBaseResponse GetSeriesBase(double id)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series/{id}", id)));
                return js.Deserialize<SeriesBaseResponse>(response);
            }

            /// <param name="id">id</param>
            /// <returns>response</returns>
            /// <summary>Returns series extended record</summary>
            public SeriesExtendedResponse GetSeriesExtended(double id, Meta2? meta)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series/{id}/extended?", id), (meta != null ? string.Format("meta={meta}", meta) : "")));
                return js.Deserialize<SeriesExtendedResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="season_type">season-type</param>
            /// <param name="page">page</param>
            /// <param name="season">season</param>
            /// <returns>response</returns>
            /// <summary>Returns series episodes from the specified season type, default returns the episodes in the series default season type</summary>
            public EpisodeResponse GetSeriesEpisodes(int page, double id, string season_type, int? season)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series/{id}/episodes/{season-type}?", id, season_type), "page=", page, (season != null ? string.Format("&season={season}", season) : "")));
                return js.Deserialize<EpisodeResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="season_type">season-type</param>
            /// <param name="page">page</param>
            /// <param name="lang">lang</param>
            /// <returns>response</returns>
            /// <summary>Returns series episodes from the specified season type and language. Default returns the episodes in the series default season type</summary>
            public EpisodeResponse GetSeriesSeasonEpisodesTranslated(int page, double id, string season_type, string lang)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series/{id}/episodes/{season-type}/{lang}?", id, season_type, lang), "page=", page));
                return js.Deserialize<EpisodeResponse>(response);
            }

            /// <param name="id">id</param>
            /// <param name="language">language</param>
            /// <returns>response</returns>
            /// <summary>Returns series translation record</summary>
            public TranslationResponse GetSeriesTranslation(double id, string language)
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, string.Format("/series/{id}/translations/{language}", id, language)));
                return js.Deserialize<TranslationResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of status records</summary>
            public SeriesStatusesResponse GetAllSeriesStatuses()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/series/statuses"));
                return js.Deserialize<SeriesStatusesResponse>(response);
            }

            /// <returns>response</returns>
            /// <summary>returns list of sourceType records</summary>
            public SourceTypeResponse GetAllSourceTypes()
            {
                string response = GetFromTvdbv4(string.Concat(BaseUrl, "/sources/types"));
                return js.Deserialize<SourceTypeResponse>(response);
            }

            ///<param name="since">since</param>
            ///<param name="type">type</param>
            ///<param name="action">action</param>
            ///<param name="page">page</param>
            /// <returns>response</returns>
            /// <summary>returns updated entities</summary>
            public UpdatesResponse Updates(double since, Type? type, Action? action, double? page)
            {
                string request = string.Concat(BaseUrl, "/updates?since=", since);

                if (type != null)
                {
                    request += string.Concat("&type=", type);
                }
                if (action != null)
                {
                    request += string.Concat("&action=", action);
                }
                if (page != null)
                {
                    request += string.Concat("&page=", page);
                }

                string response = GetFromTvdbv4(request);
                return js.Deserialize<UpdatesResponse>(response);
            }
        }
        #region Class Model
        /// <summary>An alias model, which can be associated with a series, season, movie, person, or list.</summary
        public partial class Alias
        {
            /// <summary>A 3-4 character string indicating the language of the alias, as defined in Language.</summary
            public string Language { get; set; }
            /// <summary>A string containing the alias itself.</summary
            public string Name { get; set; }

        }
        /// <summary>base artwork record</summary>
        public partial class ArtworkBaseRecord
        {
            public int Id { get; set; }
            public string Image { get; set; }
            public string Language { get; set; }
            public double Score { get; set; }
            public string Thumbnail { get; set; }
            public long Type { get; set; }
        }
        /// <summary>extended artwork record</summary>
        public partial class ArtworkExtendedRecord
        {
            public int EpisodeId { get; set; }
            public long Height { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public string Language { get; set; }
            public int MovieId { get; set; }
            public int NetworkId { get; set; }
            public int PeopleId { get; set; }
            public double Score { get; set; }
            public int SeasonId { get; set; }
            public int SeriesId { get; set; }
            public int SeriesPeopleId { get; set; }
            public string Thumbnail { get; set; }
            public long ThumbnailHeight { get; set; }
            public long ThumbnailWidth { get; set; }
            public long Type { get; set; }
            public long UpdatedAt { get; set; }
            public long Width { get; set; }

        }
        /// <summary>artwork status record</summary>
        public partial class ArtworkStatus
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>artwork type record</summary>
        public partial class ArtworkType
        {
            public long Height { get; set; }
            public long Id { get; set; }
            public string ImageFormat { get; set; }
            public string Name { get; set; }
            public string RecordType { get; set; }
            public string Slug { get; set; }
            public long ThumbHeight { get; set; }
            public long ThumbWidth { get; set; }
            public long Width { get; set; }
        }
        /// <summary>base award record</summary>
        public partial class AwardBaseRecord
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>base award category record</summary>
        public partial class AwardCategoryBaseRecord
        {
            public bool AllowCoNominees { get; set; }
            public AwardBaseRecord Award { get; set; }
            public bool ForMovies { get; set; }
            public bool ForSeries { get; set; }
            public long Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>extended award category record</summary>
        public partial class AwardCategoryExtendedRecord
        {
            public bool AllowCoNominees { get; set; }
            public AwardBaseRecord Award { get; set; }
            public bool ForMovies { get; set; }
            public bool ForSeries { get; set; }
            public long Id { get; set; }
            public string Name { get; set; }
            public ICollection<AwardNomineeBaseRecord> Nominees { get; set; }
        }
        /// <summary>extended award record</summary>
        public partial class AwardExtendedRecord
        {
            public ICollection<AwardCategoryBaseRecord> Categories { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public long Score { get; set; }
        }
        /// <summary>base award nominee record</summary>
        public partial class AwardNomineeBaseRecord
        {
            public Character Character { get; set; }
            public string Details { get; set; }
            public EpisodeBaseRecord Episode { get; set; }
            public long Id { get; set; }
            public bool IsWinner { get; set; }
            public MovieBaseRecord Movie { get; set; }
            public SeriesBaseRecord Series { get; set; }
            public string Year { get; set; }
            public string Category { get; set; }
            public string Name { get; set; }
        }
        /// <summary>biography record</summary>
        public partial class Biography
        {
            public string Biography1 { get; set; }
            public string Language { get; set; }
        }
        /// <summary>character record</summary>
        public partial class Character
        {
            public ICollection<Alias> Aliases { get; set; }
            public int EpisodeId { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public bool IsFeatured { get; set; }
            public int MovieId { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public int PeopleId { get; set; }
            public int SeriesId { get; set; }
            public long Sort { get; set; }
            public long Type { get; set; }
            public string Url { get; set; }
            public string PersonName { get; set; }
        }
        /// <summary>A company record</summary>
        public partial class Company
        {
            public string ActiveDate { get; set; }
            public ICollection<Alias> Aliases { get; set; }
            public string Country { get; set; }
            public long Id { get; set; }
            public string InactiveDate { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public long PrimaryCompanyType { get; set; }
            public string Slug { get; set; }
        }
        /// <summary>A company type record</summary>
        public partial class CompanyType
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>content rating record</summary>
        public partial class ContentRating
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Country { get; set; }
            public string ContentType { get; set; }
            public int Order { get; set; }
            public string FullName { get; set; }
        }
        /// <summary>country record</summary>
        public partial class Country
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ShortCode { get; set; }
        }
        /// <summary>Entity record</summary>
        public partial class Entity
        {
            public int MovieId { get; set; }
            public long Order { get; set; }
            public int SeriesId { get; set; }
        }
        /// <summary>Entity Type record</summary>
        public partial class EntityType
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int SeriesId { get; set; }
        }
        /// <summary>entity update record</summary>
        public partial class EntityUpdate
        {
            public string EntityType { get; set; }
            public string Method { get; set; }
            public long RecordId { get; set; }
            public long TimeStamp { get; set; }
        }
        /// <summary>base episode record</summary>
        public partial class EpisodeBaseRecord
        {
            public string Aired { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public int ImageType { get; set; }
            public long IsMovie { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public int Number { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public int Runtime { get; set; }
            public int SeasonNumber { get; set; }
            public ICollection<SeasonBaseRecord> Seasons { get; set; }
            public long SeriesId { get; set; }
        }
        /// <summary>extended episode record</summary>
        public partial class EpisodeExtendedRecord
        {
            public string Aired { get; set; }
            public int AirsAfterSeason { get; set; }
            public int AirsBeforeEpisode { get; set; }
            public int AirsBeforeSeason { get; set; }
            public ICollection<AwardBaseRecord> Awards { get; set; }
            public ICollection<Character> Characters { get; set; }
            public ICollection<ContentRating> ContentRatings { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public int ImageType { get; set; }
            public long IsMovie { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public NetworkBaseRecord Network { get; set; }
            public int Number { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public string ProductionCode { get; set; }
            public ICollection<RemoteID> RemoteIds { get; set; }
            public int Runtime { get; set; }
            public int SeasonNumber { get; set; }
            public ICollection<SeasonBaseRecord> Seasons { get; set; }
            public long SeriesId { get; set; }
            public ICollection<TagOption> TagOptions { get; set; }
            public ICollection<Trailer> Trailers { get; set; }
        }
        /// <summary>gender record</summary>
        public partial class Gender
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>base genre record</summary>
        public partial class GenreBaseRecord
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
        }
        /// <summary>language record</summary>
        public partial class Language
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string NativeName { get; set; }
            public string ShortCode { get; set; }
        }
        /// <summary>base list record</summary>
        public partial class ListBaseRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public long Id { get; set; }
            public bool IsOfficial { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public string Overview { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public string Url { get; set; }
        }
        /// <summary>extended list record</summary>
        public partial class ListExtendedRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public ICollection<Entity> Entities { get; set; }
            public long Id { get; set; }
            public bool IsOfficial { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public string Overview { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public long Score { get; set; }
            public string Url { get; set; }
        }
        /// <summary>base movie record</summary>
        public partial class MovieBaseRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public double Score { get; set; }
            public string Slug { get; set; }
            public Status Status { get; set; }
        }
        /// <summary>extended movie record</summary>
        public partial class MovieExtendedRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public ICollection<ArtworkBaseRecord> Artworks { get; set; }
            public ICollection<string> AudioLanguages { get; set; }
            public ICollection<AwardBaseRecord> Awards { get; set; }
            public string BoxOffice { get; set; }
            public string Budget { get; set; }
            public ICollection<Character> Characters { get; set; }
            public ICollection<ListBaseRecord> Lists { get; set; }
            public ICollection<GenreBaseRecord> Genres { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public string OriginalCountry { get; set; }
            public string OriginalLanguage { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public ICollection<Release> Releases { get; set; }
            public ICollection<RemoteID> RemoteIds { get; set; }
            public ICollection<ContentRating> ContentRatings { get; set; }
            public double Score { get; set; }
            public string Slug { get; set; }
            public Status Status { get; set; }
            public ICollection<StudioBaseRecord> Studios { get; set; }
            public ICollection<string> SubtitleLanguages { get; set; }
            public ICollection<TagOption> TagOptions { get; set; }
            public ICollection<Trailer> Trailers { get; set; }
            public ICollection<Inspiration> Inspirations { get; set; }
            public ICollection<ProductionCountry> ProductionCountries { get; set; }
            public ICollection<string> SpokenLanguages { get; set; }
            public Release FirstRelease { get; set; }
            public Companies Companies { get; set; }
        }
        /// <summary>base network record</summary>
        public partial class NetworkBaseRecord
        {
            public string Abbreviation { get; set; }
            public string Country { get; set; }
            public int Id { get; set; }
            public string Name { get; set; }
            public string Slug { get; set; }
        }
        /// <summary>base people record</summary>
        public partial class PeopleBaseRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public string Name { get; set; }
            public long Score { get; set; }
        }
        /// <summary>extended people record</summary>
        public partial class PeopleExtendedRecord
        {
            public ICollection<Alias> Aliases { get; set; }
            public ICollection<AwardBaseRecord> Awards { get; set; }
            public ICollection<Biography> Biographies { get; set; }
            public string Birth { get; set; }
            public string BirthPlace { get; set; }
            public ICollection<Character> Characters { get; set; }
            public string Death { get; set; }
            public int Gender { get; set; }
            public long Id { get; set; }
            public string Image { get; set; }
            public string Name { get; set; }
            public ICollection<Race> Races { get; set; }
            public ICollection<RemoteID> RemoteIds { get; set; }
            public long Score { get; set; }
            public ICollection<TagOption> TagOptions { get; set; }
        }
        /// <summary>people type record</summary>
        public partial class PeopleType
        {
            public long Id { get; set; }
            public string Name { get; set; }
        }
        /// <summary>race record</summary>
        public partial class Race
        {

        }
        /// <summary>release record</summary>
        public partial class Release
        {
            public string Country { get; set; }
            public string Date { get; set; }
            public string Detail { get; set; }
        }
        /// <summary>remote id record</summary>
        public partial class RemoteID
        {
            public string Id { get; set; }
            public long Type { get; set; }
            public string SourceName { get; set; }
        }
        /// <summary>search result</summary>
        public partial class SearchResult
        {
            public ICollection<string> Aliases { get; set; }
            public ICollection<string> Companies { get; set; }
            public string CompanyType { get; set; }
            public string Country { get; set; }
            public string Director { get; set; }
            public string ExtendedTitle { get; set; }
            public ICollection<string> Genres { get; set; }
            public string Id { get; set; }
            public string ImageUrl { get; set; }
            public string Name { get; set; }
            public string NameTranslated { get; set; }
            public string OfficialList { get; set; }
            public string Overview { get; set; }
            public ICollection<string> Overview_translated { get; set; }
            public ICollection<string> Posters { get; set; }
            public string PrimaryLanguage { get; set; }
            public string PrimaryType { get; set; }
            public string Status { get; set; }
            public ICollection<string> TranslationsWithLang { get; set; }
            public string Tvdb_id { get; set; }
            public string Type { get; set; }
            public string Year { get; set; }
            public string Thumbnail { get; set; }
            public string Poster { get; set; }
            public ICollection<TranslationSimple> Translations { get; set; }
            public bool Is_official { get; set; }
            public ICollection<RemoteID> RemoteIds { get; set; }
            public string Network { get; set; }
            public string Title { get; set; }
            public ICollection<TranslationSimple> Overviews { get; set; }
        }
        /// <summary>season genre record</summary>
        public partial class SeasonBaseRecord
        {
            public string Abbreviation { get; set; }
            public string Country { get; set; }
            public int Id { get; set; }
            public string Image { get; set; }
            public int ImageType { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public long Number { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public long SeriesId { get; set; }
            public string Slug { get; set; }
            public SeasonType Type { get; set; }
        }
        /// <summary>extended season record</summary>
        public partial class SeasonExtendedRecord
        {
            public string Abbreviation { get; set; }
            public ICollection<ArtworkBaseRecord> Artwork { get; set; }
            public ICollection<EpisodeBaseRecord> Episodes { get; set; }
            public int Id { get; set; }
            public string Image { get; set; }
            public int ImageType { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public long Number { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public long SeriesId { get; set; }
            public string Slug { get; set; }
            public ICollection<Trailer> Trailers { get; set; }
            public long Type { get; set; }
            public Companies Companies { get; set; }
            public ICollection<TagOption> TagOptions { get; set; }
        }
        /// <summary>season type record</summary>
        public partial class SeasonType
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public long Type { get; set; }
        }
        /// <summary>A series airs day record</summary>
        public partial class SeriesAirsDays
        {
            public bool Friday { get; set; }
            public bool Monday { get; set; }
            public bool Saturday { get; set; }
            public bool Sunday { get; set; }
            public bool Thursday { get; set; }
            public bool Tuesday { get; set; }
            public bool Wednesday { get; set; }
        }
        /// <summary>The base record for a series</summary>
        public partial class SeriesBaseRecord
        {
            public string Abbreviation { get; set; }
            public ICollection<Alias> Aliases { get; set; }
            public string Country { get; set; }
            public long DefaultSeasonType { get; set; }
            public string FirstAired { get; set; }
            public int Id { get; set; }
            public string Image { get; set; }
            public bool IsOrderRandomized { get; set; }
            public string LastAired { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public string NextAired { get; set; }
            public string OriginalCountry { get; set; }
            public string OriginalLanguage { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public double Score { get; set; }
            public string Slug { get; set; }
            public Status Status { get; set; }

        }
        /// <summary>The extended record for a series</summary>
        public partial class SeriesExtendedRecord
        {
            public string Abbreviation { get; set; }
            public SeriesAirsDays AirsDays { get; set; }
            public string AirsTime { get; set; }
            public ICollection<Alias> Aliases { get; set; }
            public ICollection<ArtworkExtendedRecord> Artworks { get; set; }
            public ICollection<Character> Characters { get; set; }
            public string Country { get; set; }
            public long DefaultSeasonType { get; set; }
            public string FirstAired { get; set; }
            public object Lists { get; set; }
            public ICollection<GenreBaseRecord> Genres { get; set; }
            public int Id { get; set; }
            public string Image { get; set; }
            public bool IsOrderRandomized { get; set; }
            public string LastAired { get; set; }
            public string Name { get; set; }
            public ICollection<string> NameTranslations { get; set; }
            public ICollection<Company> Companies { get; set; }
            public string NextAired { get; set; }
            public string OriginalCountry { get; set; }
            public string OriginalLanguage { get; set; }
            public ICollection<string> OverviewTranslations { get; set; }
            public ICollection<RemoteID> RemoteIds { get; set; }
            public double Score { get; set; }
            public ICollection<SeasonBaseRecord> Seasons { get; set; }
            public string Slug { get; set; }
            public Status Status { get; set; }
            public ICollection<Trailer> Trailers { get; set; }
        }
        /// <summary>source type record</summary>
        public partial class SourceType
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Postfix { get; set; }
            public string Prefix { get; set; }
            public string Slug { get; set; }
            public long Sort { get; set; }
        }
        /// <summary>status record</summary>
        public partial class Status
        {
            public long Id { get; set; }
            public bool KeepUpdated { get; set; }
            public string Name { get; set; }
            public string RecordType { get; set; }
        }
        /// <summary>studio record</summary>
        public partial class StudioBaseRecord
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public int ParentStudio { get; set; }
        }
        /// <summary>tag record</summary>
        public partial class Tag
        {
            public bool AllowsMultiple { get; set; }
            public string HelpText { get; set; }
            public long Id { get; set; }
            public string Name { get; set; }
            public ICollection<TagOption> Options { get; set; }
        }
        /// <summary>tag option record</summary>
        public partial class TagOption
        {
            public string HelpText { get; set; }
            public long Id { get; set; }
            public string Name { get; set; }
            public long Tag { get; set; }
            public string TagName { get; set; }
        }
        /// <summary>trailer record</summary>
        public partial class Trailer
        {
            public long Id { get; set; }
            public string Language { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
        }
        /// <summary>translation record</summary>
        public partial class Translation
        {
            public ICollection<string> Aliases { get; set; }
            public bool IsAlias { get; set; }
            public bool IsPrimary { get; set; }
            public string Language { get; set; }
            public string Name { get; set; }
            public string Overview { get; set; }
            /// <summary>Only populated for movie translations.  We disallow taglines without a title.</summary>
            public string Tagline { get; set; }
        }
        /// <summary>translation simple record</summary>
        public partial class TranslationSimple
        {
            public string Language { get; set; }
        }
        /// <summary>a entity with selected tag option</summary>
        public partial class TagOptionEntity
        {
            public string Name { get; set; }
            public string TagName { get; set; }
            public int TagId { get; set; }
        }
        /// <summary>Movie inspiration record</summary>
        public partial class Inspiration
        {
            public long Id { get; set; }
            public string Type { get; set; }
            public string TypeName { get; set; }
            public string Url { get; set; }
        }
        /// <summary>Movie inspiration type record</summary>
        public partial class InspirationType
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Reference_name { get; set; }
            public string Url { get; set; }
        }
        /// <summary>Production country record</summary>
        public partial class ProductionCountry
        {
            public long Id { get; set; }
            public string Country { get; set; }
            public string Name { get; set; }
        }
        /// <summary>Companies by type record</summary>
        public partial class Companies
        {
            public Company Studio { get; set; }
            public Company Network { get; set; }
            public Company Production { get; set; }
            public Company Distributor { get; set; }
            public Company SpecialEffects { get; set; }
        }
        #endregion
        /// <summary>Links for next, previous and current record</summary>
        public partial class Links
        {
            public string Prev { get; set; }
            public string Self { get; set; }
            public string Next { get; set; }
        }
        public partial class Body
        {
            public string Apikey { get; set; }
            //public string Pin { get; set; }
        }
        #region Enums
        public enum Meta
        {
            Translations = 0,
        }
        public enum Meta2
        {
            Translations = 0,
            Episodes = 1,
        }
        public enum Type
        {
            Artwork = 0,
            Award_nominees = 1,
            Companies = 2,
            Episodes = 3,
            Lists = 4,
            People = 5,
            Seasons = 6,
            Series = 7,
            Seriespeople = 8,
            Artworktypes = 9,
            Award_categories = 10,
            Awards = 11,
            Company_types = 12,
            Content_ratings = 13,
            Countries = 14,
            Entity_types = 15,
            Genres = 16,
            Languages = 17,
            Movies = 18,
            Movie_genres = 19,
            Movie_status = 20,
            Peopletypes = 21,
            Seasontypes = 22,
            Sourcetypes = 23,
            Tag_options = 24,
            Tags = 25,
            Translatedcharacters = 26,
            Translatedcompanies = 27,
            Translatedepisodes = 28,
            Translatedlists = 29,
            Translatedmovies = 30,
            Translatedpeople = 31,
            Translatedseasons = 32,
            Translatedserierk = 33,
        }
        public enum Action
        {
            Delete = 0,
            Update = 1,
        }
        #endregion
        #region Responses
        public partial class LoginResponse
        {
            public Data Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ArtworkBaseResponse
        {
            public ArtworkBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ArtworkExtendedResponse
        {
            public ArtworkExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ArtworkStatusListResponse
        {
            public ICollection<ArtworkStatus> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ArtworkTypeListResponse
        {
            public ICollection<ArtworkType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class AwardBaseListResponse
        {
            public ICollection<AwardBaseRecord> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class AwardBaseResponse
        {
            public AwardBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class AwardExtendedResponse
        {
            public AwardExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class AwardCategoryBaseResponse
        {
            public AwardCategoryBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class AwardCategoryExtendedResponse
        {
            public AwardCategoryExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class CharacterResponse
        {
            public Character Data { get; set; }
            public string Status { get; set; }
        }
        public partial class CompanyListResponse
        {
            public ICollection<Company> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class CompanyTypeListResponse
        {
            public ICollection<CompanyType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class CompanyResponse
        {
            public Company Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ContentRatingResponse
        {
            public ICollection<ContentRating> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class CountryResponse
        {
            public ICollection<Country> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class EntityTypeResponse
        {
            public ICollection<EntityType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class EpisodeBaseResponse
        {
            public EpisodeBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class EpisodeExtendedResponse
        {
            public EpisodeExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class TranslationResponse
        {
            public Translation Data { get; set; }
            public string Status { get; set; }
        }
        public partial class GenderListResponse
        {
            public ICollection<Gender> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class GenderBaseListResponse
        {
            public ICollection<GenreBaseRecord> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class GenreBaseResponse
        {
            public GenreBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class InspirationTypeResponse
        {
            public ICollection<InspirationType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class LanguageListResponse
        {
            public ICollection<Language> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ListBaseListResponse
        {
            public ICollection<ListBaseRecord> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ListBaseResponse
        {
            public ListBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class ListExtendedResponse
        {
            public ListExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class MovieBaseListResponse
        {
            public ICollection<MovieBaseRecord> Data { get; set; }
            public string Status { get; set; }
            public Links Links { get; set; }
        }
        public partial class MovieBaseResponse
        {
            public MovieBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class MovieExtendedResponse
        {
            public MovieExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class StatusResponse
        {
            public ICollection<Status> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class PeopleBaseResponse
        {
            public PeopleBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class PeopleExtendedResponse
        {
            public PeopleExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class PeopleTypeResponse
        {
            public ICollection<PeopleType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SearchResultResponse
        {
            public ICollection<SearchResult> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeasonBaseListResponse
        {
            public ICollection<SeasonBaseRecord> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeasonBaseResponse
        {
            public SeasonBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeasonExtendedResponse
        {
            public SeasonExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeasonTypeResponse
        {
            public SeasonType Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeriesBaseListResponse
        {
            public ICollection<SeriesBaseRecord> Data { get; set; }
            public string Status { get; set; }
            public Links Links { get; set; }
        }
        public partial class SeriesBaseResponse
        {
            public SeriesBaseRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeriesExtendedResponse
        {
            public SeriesExtendedRecord Data { get; set; }
            public string Status { get; set; }
        }
        public partial class EpisodeResponse
        {
            public Data2 Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SeriesStatusesResponse
        {
            public ICollection<Status> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class SourceTypeResponse
        {
            public ICollection<SourceType> Data { get; set; }
            public string Status { get; set; }
        }
        public partial class UpdatesResponse
        {
            public ICollection<EntityUpdate> Data { get; set; }
            public string Status { get; set; }
            public Links Links { get; set; }
        }
        #endregion
        public partial class Data
        {
            public string Token { get; set; }
        }
        public partial class Data2
        {
            public SeriesExtendedRecord Series { get; set; }
            public ICollection<EpisodeBaseRecord> Episodes { get; set; }

        }
        
    }
}