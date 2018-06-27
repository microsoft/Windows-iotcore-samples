using Keg.DAL;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.UWP.Utils
{
    public class SqLiteHelper
    {
        public static string SqliteConnectionString = "Filename=KegSqLite.sds";
        //public static string SqliteConnectionString = $"Filename= {System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "KegSqLite.sds") }";
        public static void InitializeSqLiteDatabase()
        {
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();

                    String kegVisitorCommand = "CREATE TABLE IF NOT " +
                        "EXISTS Keg_Visitor (Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, VisitorId TEXT NOT NULL, Ounces REAL NOT NULL, VisitedDateTime TEXT NOT NULL)";

                    SqliteCommand kebVisitorTable = new SqliteCommand(kegVisitorCommand, db);

                    kebVisitorTable.ExecuteReader();
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogException(ex, "SqLiteHelper:InitializeSqLiteDatabase", SeverityLevel.Critical);
                throw;
            }
        }

        public Int32 AddPersonConsumption(string userId, float ounces)
        {
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    string sql = "INSERT INTO Keg_Visitor (VisitorId, Ounces, VisitedDateTime) VALUES (@visitorId, @ounces, @visitedDateTime);";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        command.Parameters.AddWithValue("@visitorId", userId);
                        command.Parameters.AddWithValue("@ounces", ounces);
                        command.Parameters.AddWithValue("@visitedDateTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        command.Prepare();

                        return command.ExecuteNonQuery();

                    }
                }
            }
            catch (Exception ex)
            {
                //KegLogger.KegLogException(ex, "SqLiteHelper:AddPersonConsumption", SeverityLevel.Error);

                KegLogger.KegLogTrace(ex.Message, "SqLiteHelper:AddPersonConsumption", SeverityLevel.Error,
                    new Dictionary<string, string>()
                    {
                        { "UserID", userId }, {"Ounces", ounces.ToString()}
                    });
                throw;
            }

        }


        /// <summary>
        /// returns the total consumption in last 24 hours
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Double GetPersonConsumption(string userId)
        {
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    string sql = "SELECT total(Ounces) FROM Keg_Visitor WHERE VisitorId = @visitorId;";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        command.Parameters.AddWithValue("@visitorId", userId);
                        var reader = command.ExecuteScalar();
                        //return 0 or number
                        return Double.Parse(reader.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogTrace(ex.Message, "SqLiteHelper:GetPersonConsumption", SeverityLevel.Error,
                new Dictionary<string, string>()
                {
                    { "UserID", userId }
                });
                throw;
            }

        }

        public Int32 GetVisitedPersonsCount(Int32 sinceHours)
        {
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    string sql = $"SELECT count(distinct(VisitorId)) FROM Keg_Visitor WHERE strftime('%s', datetime(VisitedDateTime,'{sinceHours} hour')) >= strftime('%s', datetime('now', 'localtime'));";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        var count = command.ExecuteScalar();
                        //return 0 or number
                        return Int32.Parse(count.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogException(ex, "SqLiteHelper:GetVisitedPersonsCount", SeverityLevel.Error);
                throw;
            }
        }


        public IEnumerable<string> GetVisitedPersons(Int32 sinceHours)
        {
            List<string> persons = new List<string>();
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    string sql = $"SELECT distinct(VisitorId) FROM Keg_Visitor WHERE strftime('%s', datetime(VisitedDateTime,'{sinceHours} hour')) >= strftime('%s', datetime('now', 'localtime'));";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        var reader = command.ExecuteReader();
                        while (reader.HasRows && reader.Read())
                        {
                            persons.Add(reader[0].ToString());
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogException(ex, "SqLiteHelper:GetVisitedPersons", SeverityLevel.Error);
                throw;
            }
            return persons;

        }

        public void LogExpiredUserConsumption(Int32 sinceMinutes)
        {
            Dictionary<string, double> persons = new Dictionary<string, double>();
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    //select VisitedDateTime, datetime('now', 'localtime'), strftime('%s', VisitedDateTime), strftime('%s', datetime(VisitedDateTime,'1 hour')), strftime('%s', datetime('now', 'localtime')) FROM Test  WHERE strftime('%s', datetime(VisitedDateTime,'1 hour')) < strftime('%s', datetime('now', 'localtime'))
                    //string sql = $"SELECT VisitorId, SUM(Ounces) FROM Keg_Visitor WHERE strftime('%s', datetime(VisitedDateTime,'{sinceHours} hour')) < strftime('%s', datetime('now', 'localtime')) Group By VisitorId Having Sum(Ounces)> 0.0;";
                    string sql = $"SELECT VisitorId, SUM(Ounces) FROM Keg_Visitor WHERE strftime('%s', datetime(VisitedDateTime,'{sinceMinutes} minutes')) < strftime('%s', datetime('now', 'localtime')) Group By VisitorId Having Sum(Ounces)> 0.0;";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        var reader = command.ExecuteReader();
                        while (reader.HasRows && reader.Read())
                        {
                            if(!reader.IsDBNull(0))
                            {
                                persons.Add(
                                    reader[0].ToString(), reader.IsDBNull(1) ? 0.0 : double.Parse(reader[1].ToString()));

                                //Log Metrics
                                MetricTelemetry metricTelemetry = new MetricTelemetry();
                                metricTelemetry.Name = reader[0].ToString();
                                metricTelemetry.Sum = reader.IsDBNull(1) ? 0.0 : double.Parse(reader[1].ToString());
                                metricTelemetry.Context.Operation.Name = "EventComplete";

                                KegLogger.KegLogMetrics("Event Complete or timeout", "EventComplete", metricTelemetry);
                            }

                        }

                    }
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogTrace(ex.Message, "SqLiteHelper:DeleteExpiredUserConsumption", SeverityLevel.Error,
                    new Dictionary<string, string>() {
                       {"Con", SqliteConnectionString }
                   });
                //throw;
            }

        }



        /// <summary>
        /// Delete entries related to user of their old consumption
        /// </summary>
        /// <param name="sinceHours"></param>
        public void DeleteExpiredUserConsumption(Int32 sinceMinutes)
        {
            try
            {
                using (SqliteConnection db =
                    new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();
                    //select VisitedDateTime, datetime('now', 'localtime'), strftime('%s', VisitedDateTime), strftime('%s', datetime(VisitedDateTime,'1 hour')), strftime('%s', datetime('now', 'localtime')) FROM Test  WHERE strftime('%s', datetime(VisitedDateTime,'1 hour')) < strftime('%s', datetime('now', 'localtime'))
                    string sql = $"DELETE FROM Keg_Visitor WHERE strftime('%s', datetime(VisitedDateTime,'{sinceMinutes} minutes')) < strftime('%s', datetime('now', 'localtime'));";

                    // Commit results.
                    using (SqliteCommand command = new SqliteCommand(sql, db))
                    {
                        var result = command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                KegLogger.KegLogTrace(ex.Message, "SqLiteHelper:DeleteExpiredUserConsumption", SeverityLevel.Error,
                    new Dictionary<string, string>() {
                       {"Con", SqliteConnectionString }
                   });
                //throw;
            }

        }


        public Int32 SetKegEvent(Int32? eventId)
        {
            using (SqliteConnection db =
                new SqliteConnection(SqliteConnectionString))
            {
                db.Open();
                string sql = string.Empty;

                if(eventId.HasValue)
                {
                    sql = "REPALCE INTO Keg_Events (Id, EndTime) VALUES (@eventId, @endTime);";
                } else
                {
                    sql = "INSERT INTO Keg_Events (StartTime) VALUES (@startTime);";
                }
                
                // Commit results.
                using (SqliteCommand command = new SqliteCommand(sql, db))
                {
                    if(eventId.HasValue)
                    {
                        command.Parameters.AddWithValue("@eventId", eventId.Value);
                        command.Parameters.AddWithValue("@endTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    } else
                    {
                        command.Parameters.AddWithValue("@startTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    
                    command.Prepare();

                    return command.ExecuteNonQuery();

                }
            }

        }


        public Tuple<Int32, string, string> GetActiveEvent()
        {
            using (SqliteConnection db =
                new SqliteConnection(SqliteConnectionString))
            {
                db.Open();
                string sql = $"SELECT Id, StartTime, EndTime FROM Keg_Events ORDER BY Id DESC LIMIT 1;";

                // Commit results.
                using (SqliteCommand command = new SqliteCommand(sql, db))
                {
                    var reader = command.ExecuteReader();

                    if(reader.HasRows)
                    {
                        //return 0 or number
                        return new Tuple<Int32, string, string>(
                            reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                            reader.IsDBNull(2) ? string.Empty : reader.GetString(2));
                    }
                    else
                    {
                        return null;
                    }
                }
            }

        }


        public void Clean()
        {
            String tableDropCommand = "DROP TABLE IF EXISTS Keg_Visitor";

            try
            {
                using (SqliteConnection db =
                      new SqliteConnection(SqliteConnectionString))
                {
                    db.Open();

                    SqliteCommand dropTable = new SqliteCommand(tableDropCommand, db);

                    dropTable.ExecuteReader();

                    InitializeSqLiteDatabase();
                }
            }
            catch(Exception ex)
            {
                KegLogger.KegLogTrace(ex.Message, "SqLiteHelper:Clean", SeverityLevel.Error,
                    new Dictionary<string, string>() {
                       {"Con", SqliteConnectionString }
                   });
                throw;
            }
        }
    }
}
