
using Newtonsoft.Json;
using System.Text;

public class Corporate
{
    private string Organisasjonsnummer;
    private string Navn;
    private string AntallAnsatte;
    private string Naeringskode;
    private string Organisasjonsform;
    private string BrregNavn;
    private string StatusCode;
    private string Status;

    public Corporate(string Organisasjonsnummer, string Navn, string AntallAnsatte, string Naeringskode, string Organisasjonsform, string BrregNavn, string StatusCode, string Status)
    {
        this.Organisasjonsnummer = Organisasjonsnummer;
        this.Navn = Navn;
        this.AntallAnsatte = AntallAnsatte;
        this.Naeringskode = Naeringskode;
        this.Organisasjonsform = Organisasjonsform;
        this.BrregNavn = BrregNavn;
        this.StatusCode = StatusCode;
        this.Status = Status;
    }

    public string getNavn
    {
        get { return Navn; }
    }

    public string getOrgnr
    {
        get { return Organisasjonsnummer; }
    }

    public string getStatusCode
    {
        get { return StatusCode; }
    }

    public string showAll
    {
        get { return Organisasjonsnummer + '\t' + Navn + '\t' + AntallAnsatte + '\t' + Naeringskode + '\t' + Organisasjonsform + '\t' + BrregNavn + '\t' + StatusCode + '\t' + Status; }
    }

    public string createCSVRow
    {
        get { return Organisasjonsnummer + ';' + Navn + ';' + AntallAnsatte + ';' + Naeringskode + ';' + Organisasjonsform + ';' + BrregNavn + ';' + StatusCode + ';' + Status; }
    }

    public string createErrorLogCSVRow
    {
        get { return Organisasjonsnummer + ';' + StatusCode; }
    }

}

class Program
{
    static void Main()
    {
        List<Corporate> corporates = extractOrgnr();
        retriveCorporateData(corporates);
        Console.ReadLine();
    }

    static HttpClient _client = new HttpClient();

    static async Task<Corporate> GetCorporate(Corporate corporate)
    {
        try
        {
            HttpResponseMessage response = await _client.GetAsync("https://data.brreg.no/enhetsregisteret/api/enheter/" + corporate.getOrgnr);
            string statusCode = ((int)response.StatusCode).ToString();
            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                dynamic objects = JsonConvert.DeserializeObject(result);
                string status = "";
                if(HasProperty(objects, "konkurs"))
                {
                    status = "konkurs: " + Convert.ToString(objects.konkurs);
                }
                if (HasProperty(objects, "slettedato"))
                {
                    status += " slettdato: " + Convert.ToString(objects.slettedato);
                }
                string brreg = "";
                if(HasProperty(objects, "navn"))
                {
                    if (corporate.getNavn.ToLower() != (Convert.ToString(objects.navn)).ToLower())
                    {
                        brreg = Convert.ToString(objects.navn);
                    }
                }
                
                return new Corporate(
                    corporate.getOrgnr, 
                    corporate.getNavn, 
                    HasProperty(objects, "antallAnsatte") ? Convert.ToString(objects.antallAnsatte) : "",
                    HasProperty(objects, "naeringskode1") ? Convert.ToString(objects.naeringskode1.kode) : "",
                    HasProperty(objects, "organisasjonsform") ? Convert.ToString(objects.organisasjonsform.kode) : "", 
                    brreg,
                    statusCode, 
                    status);
            }
            else { 
                string result = await response.Content.ReadAsStringAsync();
                dynamic objects = JsonConvert.DeserializeObject(result); 
                return new Corporate(corporate.getOrgnr, corporate.getNavn, "", "", "", "", statusCode, "");
            
            }
        }
        catch (Exception e) 
        {
            Console.WriteLine(e.Message);
            return new Corporate(corporate.getOrgnr, corporate.getNavn, "", "", "", "", "", e.Message);
        }



    }

    public static bool HasProperty(dynamic obj, string name)
    {
        try
        {
            var value = obj[name];
            if (value == null)
                return false;
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    static async void retriveCorporateData(List<Corporate> corporates)
    {
        var csv = new StringBuilder();
        var csvErrorLog = new StringBuilder();

        // Add CSV header
        csv.AppendLine("Organisasjonsnummer;Navn;AntallAnsatte;Naeringskode;Organisasjonsform;BrregNavn;StatusCode;Status");
        csvErrorLog.AppendLine("Organisasjonsnummer;StatusCode");


        for (int i= 0; i < corporates.Count; i++)
        {
            
            Corporate corporate = await GetCorporate(corporates[i]);
            
            if(corporate.getStatusCode != "200")
            {
                csvErrorLog.AppendLine(corporate.createErrorLogCSVRow);
            }
            csv.AppendLine(corporate.createCSVRow);
        }
        Console.WriteLine(csv.ToString());
        createCSV(@"C:\po-kunder-with-data.csv", csv.ToString());
        createCSV(@"C:\errorLog.csv", csvErrorLog.ToString());
    }

    static void createCSV(string filePath, string csv)
    {
        File.WriteAllText(filePath, csv.ToString());
    }

    static List<Corporate> extractOrgnr()
    {
        string path = @"C:\po-kunder-short.csv";
        StreamReader reader = new StreamReader(path);
        List<Corporate> corporateListe = new List<Corporate>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            string[] values = line.Split(';');

            if (values[0] != "OrgNo")
            {
                corporateListe.Add(new Corporate(values[0], values[1], null, null, null, null, null, null));
            }
        }
        for (int i = 0; i < corporateListe.Count; i++)
        {
            Console.WriteLine(corporateListe[i].getNavn);
        }
        return corporateListe;
    }
}

