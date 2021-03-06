﻿using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurityList
{
    public class SQLiteSecurities
    {

        /// <summary>
        /// Возвращает список инструментов из базы данных гидры 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<Security> GetSecurities(string path)
        {
            DataSet data = new DataSet();
            var securities = new List<Security>();

            try
            {

                // Создаем соединение и получаем данные из таблицы Security, которые заполняют Dataset 
                using (SQLiteConnection conn = new SQLiteConnection(string.Format("Data Source={0}; Version=3;", path)))
                {

                    SQLiteCommand cmd = new SQLiteCommand(conn);
                    cmd.CommandText = "SELECT Id, Board, PriceStep, Decimals, Type, ExpiryDate, UnderlyingSecurityId, Strike, OptionType FROM Security";
                    data.Reset();
                    SQLiteDataAdapter ad = new SQLiteDataAdapter(cmd);
                    ad.Fill(data);
                }

                foreach (DataRow row in data.Tables[0].AsEnumerable())
                {
                    securities.Add(CreateSecurity(row));
                }

            }
            catch (Exception ex)
            {
                securities = null;
                Debug.WriteLine(ex);
            }

            return securities;

        }

        // Преобразует данные DataRow таблицы в Security
        private static Security CreateSecurity(DataRow row)
        {
            var id = row.Field<string>("Id");
            var board = row.Field<string>("Board");
            var priceStep = row.Field<double?>("PriceStep");
            var decimals = row.Field<long?>("Decimals");
            var typeInt = row.Field<long?>("Type");

            SecurityTypes? type = null;

            if (typeInt != null)
                type = (SecurityTypes)typeInt;

            var expDate = row.Field<string>("ExpiryDate");
            var underlyingSecurityId = row.Field<string>("UnderlyingSecurityId");
            var strike = row.Field<double?>("Strike");
            var optTypeInt = row.Field<long?>("OptionType");

            var security = new Security
            {
                Id = id,
                Board = ExchangeBoard.GetOrCreateBoard(board),
                PriceStep = (decimal?)priceStep,
                Decimals = (int?)decimals,
                Type = type
            };

            if (security.Type == SecurityTypes.Future || security.Type == SecurityTypes.Option)
            {
                security.ExpiryDate = string.IsNullOrWhiteSpace(expDate) ? null : (DateTimeOffset?)DateTimeOffset.Parse(expDate);
                security.UnderlyingSecurityId = underlyingSecurityId;

                if (security.Type == SecurityTypes.Option)
                {
                    security.Strike = (decimal?)strike;

                    OptionTypes? optType = null;

                    if (optTypeInt != null)
                        optType = (OptionTypes)optTypeInt;

                    security.OptionType = optType;

                }

            }

            return security;
        }

    }
}
