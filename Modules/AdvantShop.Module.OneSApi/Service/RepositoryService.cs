using AdvantShop.Configuration;
using AdvantShop.Core.SQL;
using AdvantShop.Helpers;
using AdvantShop.Repository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace AdvantShop.Module.OneSApi.Service
{
    public class RepositoryService
    {

        public static List<IpZone> GetIpZonesByCity(string cityName)
        {
            string translitRu = StringHelper.TranslitToRus(cityName);
            string translitKeyboard = StringHelper.TranslitToRusKeyboard(cityName);

            return SQLDataAccess.ExecuteReadList<IpZone>(
                "Select Top (10) CityName, CityID, Zip, District, " +
                //"(case when (select count(*) from [Customers].[City] where CityName = Cities.CityName) > 1 then (Region.RegionName) else '' end) as RegionName, " +
                "Region.RegionName, Region.RegionId, Country.CountryId, Country.CountryName " +
                "From Customers.City as Cities INNER JOIN Customers.Region ON Region.RegionID = Cities.RegionId INNER JOIN Customers.Country ON Country.CountryID = Region.CountryID " +
                "WHERE (CityName like @name + '%' OR CityName like @translitRu + '%' OR CityName like @translitKeyboard + '%') " +
                $"And Region.CountryId = {SettingsMain.SellerCountryId} " +
                "order by Cities.DisplayInPopup DESC, Cities.CitySort ASC, CityName",
                CommandType.Text, reader => new IpZone()
                {
                    CityId = SQLDataHelper.GetInt(reader, "CityID"),
                    City = SQLDataHelper.GetString(reader, "CityName"),
                    District = SQLDataHelper.GetString(reader, "District"),
                    Zip = SQLDataHelper.GetString(reader, "Zip"),
                    RegionId = SQLDataHelper.GetInt(reader, "RegionId"),
                    Region = SQLDataHelper.GetString(reader, "RegionName"),
                    CountryId = SQLDataHelper.GetInt(reader, "CountryId"),
                    CountryName = SQLDataHelper.GetString(reader, "CountryName")
                },
                new SqlParameter("@name", cityName),
                new SqlParameter("@translitRu", translitRu),
                new SqlParameter("@translitKeyboard", translitKeyboard));
        }

    }
}
