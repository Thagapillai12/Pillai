using AT.Core.BasicConfiguration;
using AT.Core.Objects;
using System;
using System.Data;
using System.Data.SqlClient;

namespace WK.DE.TestFramework
{
    public class ResetDatabase
    {
        private static IDbConnection GetConnectionATSystem()
        {
            var officeConfiguration = TestTools.GetConfigurationInformation().Item1;
            string connectionString = BasicSystemConfiguration.GetConnectionString(officeConfiguration);
            return new SqlConnection(connectionString);
        }

        /// <summary>
        /// Ruft eine Verbindung zur Datenbank ab.
        /// </summary>
        public static IDbConnection GetConnection()
        {
            var officeConfiguration = TestTools.GetConfigurationInformation().Item2;
            string connectionString = BasicSystemConfiguration.GetConnectionString(officeConfiguration);
            return new SqlConnection(connectionString);
        }

        public static void ResetOnlineAkte()
        {
            using (IDbConnection dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("PAPGLSET", null);
                dataConnection.DeleteAndPackTable("AWSNOTI", null);
                dataConnection.DeleteAndPackTable("APACCT", null);
                dataConnection.DeleteAndPackTable("APUSTOCL", null);
            }
        }

        public static void ResetNationalitaeten()
        {
            using (IDbConnection dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("PAMNATIO", null);
            }
        }

        public static void ResetPostmappen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PSTMAPST", null);
                dataConnection.DeleteAndPackTable("PSTMAP", null);
                dataConnection.DeleteAndPackTable("BLBDOCPM", null);
                dataConnection.DeleteAndPackTable("PSTMAPAD", null);
                //dataConnection.DeleteAndPackTable("PSTMAPIN", null);
            }
        }

        public static void ResetAkten(bool reseed = false)
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("STAMM", reseed);
            }
        }

        public static void ResetUrkunden(bool reseed = false)
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("URKUND", reseed);
            }
        }

        public static long ResetAktenAndCreateDummy(bool reseed = false)
        {
            ResetAkten(reseed);

            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("STAMM", reseed);
            }
            return CreateDummyCase(0);
        }

        public static long CreateDummyCase(int number)
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                using (var command = dataConnection.CreateCommand())
                {
                    var caseNumber = (42 + number).ToString().PadLeft(5, '0') + "/21";
                    command.CommandText = String.Format("insert into STAMM (D_AZ) values ('{0}'); select @@identity;", caseNumber);
                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }

        public static void ResetAdressaten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("MAND2", null);
            }
        }

        public static void ResetFamilienstatus()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("M2STATUS", null);
            }
        }

        public static void ResetAkteBeteiligte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("CMCATOAD", null);
            }
        }

        public static void ResetRechnungshauptkopf()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKINLINK", null);
            }
        }

        public static void ResetRechnungskopf()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKINHEAD", null);
            }
        }

        public static void ResetRechnungskopfAZ()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKINVLST", null);
            }
        }

        public static void ResetRechnungspositionen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("RNGPOS", null);
            }
        }

        public static void ResetRechnungWertermittlung()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("RNGWE", null);
            }
        }

        public static void ResetRechnungsdetails()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKILSDET", null);
            }
        }

        public static void ResetParameterAnredeformen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PAMADRBY", null);

                using (var command = dataConnection.CreateCommand())
                {
                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Behörde', '', 'Sehr geehrte Damen und Herren,', '2', 'BEHÖRDE')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Eheähnliche Gemeinschaft', '', 'Sehr geehrte Frau *," + Environment.NewLine + "sehr geehrter Herr *,', '4', 'EHEÄHNL')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Eheleute', 'Eheleute', 'Sehr geehrte Frau *," + Environment.NewLine + "sehr geehrter Herr *,', '3', 'EHELEUT')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'GmbH', 'Firma', 'Sehr geehrte Damen und Herren,', '12', 'GMBH')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Frau', 'Frau', 'Sehr geehrte Frau *,', '17', 'FRAU')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (1, 'Mann', 'Herrn', 'Sehr geehrter Herr *,', '17', 'MANN')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Minderjähriges Mädchen', 'Frau', 'Sehr geehrte Frau *,', '16', 'MINWEIB')";
                    command.ExecuteNonQuery();

                    command.CommandText = "insert into PAMADRBY (D_AMAB_AMAD_AUTO, D_AMAB_NAME, D_AMAB_NAMEADD, D_AMAB_ADDRESSBY, D_AMAB_LEGALSTR, D_AMAB_KZ) values (0, 'Minderjähriger Junge', 'Herrn', 'Sehr geehrter Herr *,', '16', 'MINJUNG')";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ResetParameterBeteiligungsarten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMCRELA", null);
            }
        }

        public static void ResetParameterSachgebiete()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMSACH", null);
            }
        }

        public static void ResetParameterStandorte(bool reseed = false)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMSTORT", reseed);
            }
        }

        public static void ResetManuellErfassteKopien()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PKELE", null);
            }
        }

        public static void ResetManuellErfassteTelekomkosten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("JOURNAL", null);
            }
        }

        public static void ResetManuellErfassteGebuehren()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PGEBUE", null);
            }
        }

        public static void ResetManuellErfassteReisekosten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKTRACO", null);
            }
        }

        public static void ResetOnlineAktePasswordHistory()
        {
            using (IDbConnection dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("OAPWDHISTORY", null);
            }
        }

        public static void ResetManuellErfassteSteuerfreieAuslagen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PSTFRA", null);
            }
        }

        public static void ResetMahnmodalitaeten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PBKDUNNI", null);
            }
        }

        public static void ResetParameterAbrechnung()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PINPARAM", null);
            }
        }

        public static void ResetMassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCEVENT", null);
            }
        }

        public static void ResetBuchhaltunsMandant()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKCLIENT", null);
            }
        }

        public static void ResetMassnahmeMassnahme()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCEVTOEV", null);
            }
        }

        public static void ResetErfassteZeiten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBSHEET", null);
            }
        }

        public static void ResetBuerogemeinschaft(bool createDefaultRow, bool reseed = false, int seed = 0)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("OFFICE", reseed, seed);
                dataConnection.DeleteAndPackTable("OFFICEEX", reseed);

                if (createDefaultRow)
                {
                    using (var command = dataConnection.CreateCommand())
                    {
                        // D_OF_BE_PBERI muss auf 0 gesetzt werden, da NULL von den Stored Procedures nicht richtig intepretiert wird
                        command.CommandText = "insert into OFFICE (D_OF_SY_USEOFDOCDB, D_OF_SO_WEGEN, D_OF_BE_PBERI) values (2, '0', '0')";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void ResetMitarbeiter(bool reseed = false)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("[USER]", reseed);
            }
        }

        public static void ResetRechnungsnummer(bool createEmptyRecord)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("RECHNGNR", null);

                if (createEmptyRecord)
                {
                    using (var command = dataConnection.CreateCommand())
                    {
                        command.CommandText = "insert into RECHNGNR (D_NULLRECH, D_USERRECH) values (0, '')";
                        command.ExecuteNonQuery();
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void ResetStatusGrundbuch()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKGRSTAT", null);
            }
        }

        public static void ResetGrundbuchNeu()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKGRUND", null);
            }
        }

        public static void ResetAnteilUmsatzrelevanterMitarbeiter()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKGRTOUS", null);
            }
        }

        public static void ResetZusatzangaben()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMADDIH", null);
                dataConnection.DeleteAndPackTable("PCMADDII", null);
                dataConnection.DeleteAndPackTable("CMADDI", null);
            }
        }

        public static void ResetAnteilMitarbeiterAkte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("CMCATOUS", null);
            }
        }

        public static void ResetAnteilMitarbeiterRechnung()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKINTOUS", null);
            }
        }

        public static void ResetGrundbuch(string mandant, string mmjj)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();
                string name = "BUCH_" + mandant + "_GRBU" + mmjj + "_GRUNDB";
                dataConnection.DeleteAndPackTable(name, null);
            }
        }

        public static void ResetMassnahmenGrundbuch()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCEVTOGB", null);
            }
        }

        public static void ResetGrundbuchMandant()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCGBTOMA", null);
            }
        }

        public static void ResetBuchhaltungsMandant()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKCLIENT", null);
            }
        }

        public static void ResetKostenstellen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("BKKOSTST", null);
            }
        }

        public static void ResetMehrwertsteuertabelle()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("SDCMWST", null);
            }
        }

        public static void ResetSachkontenzuordnung()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("SDCCOMA4", null);
            }
        }

        public static void ResetParameterReferate()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMREFE", null);
            }
        }

        public static void ResetParameterRechtsanwaelteNotare(bool reseed = false)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMRANO", reseed);
            }
        }

        public static void ResetParameterRechtsanwaelteNotareReferat()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PCMRANRE", null);
            }
        }

        public static void ResetParameterMassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("SDCMEASU", null);
            }
        }

        public static void ResetVorschlagsrechnungen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBTBILL", null);
            }
        }

        public static void ResetRechnungsnummernTB()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBRNR", null);
            }
        }

        public static void ResetUmbuchungenTB()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBUMB", null);
            }
        }

        public static void ResetAnteilTBUmsatzrelevanterMitarbeiter()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBTBILSP", null);
            }
        }

        public static void ResetAnteilTBZuarbeitenderMitarbeiter()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBBILSP2", null);
            }
        }

        public static void ResetStatistikErfassteZeiten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TBSTASHE", null);
            }
        }

        public static void ResetMeldungRechnung()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCINVRPT", null);
            }
        }

        public static void ResetParameterTodoMassnahmnen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PTODO", null);
            }
        }

        public static void ResetToDos()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("TODO", null);
                dataConnection.DeleteAndPackTable("TODOREL", null);
                dataConnection.DeleteAndPackTable("PTODO", null);

                using (var command = dataConnection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (1, 'Berufung', 0, 0, 3, 1, 1, 5, 10, 0, 0, 0, '', 1, 0, 1, 1)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (2, 'Mündliche Verhandlung', 0, 0, 10, 0, 0, 0, 0, 0, 0, 0, '', 4, 0, 0, 1)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (4, 'Mahn-Wiedervorlage', 0, 0, 0, 10, 1, 0, 0, 0, 0, 0, '', 0, 0, 0, 1)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (8, 'Posteingang', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '', 4, 0, 0, 1)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (16, 'Telefonrückruf', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '', 4, 0, 0, 1)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO [dbo].[PTODO] ([D_PTOD_GROUP] ,[D_PTOD_NAME] ,[D_PTOD_TDDES_AUTO] ,[D_PTOD_TDDIS_AUTO] ,[D_PTOD_TYP] ,[D_PTOD_TOADD] ,[D_PTOD_CHK_HOLIDAYS] ,[D_PTOD_TERVOF] ,[D_PTOD_TERMIF] ,[D_PTOD_REMIND] ,[D_PTOD_REMIND_TIME] ,[D_PTOD_ZHON] ,[D_PTOD_TYP_STEP] ,[D_PTOD_TBJT_AUTO] ,[D_PTOD_AUTO_PRINT] ,[D_PTOD_TDFRI_AUTO] ,[D_PTOD_VALID]) VALUES (4096, 'Aufforderungsschreiben', 0, 0, 56, 0, 0, 0, 0, 0, 0, 0, '', 0, 0, 0, 1)";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ResetParameterEntfernungszonenTodoMassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PTODODIS", null);
            }
        }

        public static void ResetParameterBeschreibungTodoMassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PTODODES", null);
            }
        }

        public static void ResetDokumentInhalt(DocumentContentStorageTypeToReadFrom contentSaveType)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DO000001_DOCDB", null);
                dataConnection.DeleteAndPackTable("BLBDOCDB", null);

                dataConnection.DeleteAndPackTable("OFFICE", null);
                using (var command = dataConnection.CreateCommand())
                {
                    command.CommandText = "insert into OFFICE (D_OF_SY_USEOFDOCDB, D_OF_SO_WEGEN) values (" + ((int)contentSaveType).ToString() + ", '0')";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ResetDokumentenMassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("HMEASURE", null);

                using (var command = dataConnection.CreateCommand())
                {
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via AdvoAssist', 'PADV', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Postweg', 'POS1', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via EMail', 'POS2', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Fax', 'POS3', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Dateiimport', 'POS4', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Elek. Schadenabwicklung', 'POS5', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via OnlineAkte', 'POS6', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via beA', 'POS7', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Notariat', 'POS8', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Posteingang via Smarte Anwaltsakte', 'POS9', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Postausgang via beA', 'PBEA', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Postausgang via beBPo', 'PBPA', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                    command.CommandText = "insert into HMEASURE ([D_HM_NAME], [D_HM_QCODE], [D_HM_ANNOTEXT], [D_HM_COMMENT], [D_HM_DTYPE], [D_HM_DDAYS], [D_HM_DYNAMIC], [D_HM_SSD]) values ('Postausgang beBPo', 'PBPO', 1, '', 0, 0, 0, 0)";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void ResetDocumentHeadHistoryAndLinks(bool reseed = false)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("HISTORY", null, reseed);
                dataConnection.DeleteAndPackTable("DOCHEAD", null, reseed);
                dataConnection.DeleteAndPackTable("DOCAUDIT", null, reseed);
                dataConnection.DeleteAndPackTable("DMSCHECK", null, reseed);
                dataConnection.DeleteAndPackTable("DHTOHI", null, reseed);
                dataConnection.DeleteAndPackTable("SUBHISTO", null, reseed);
            }
        }

        public static void ResetBankkonten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PBKEACCT", null);
            }
        }

        public static void ResetAnsprechpartner()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("AMPART", null);
            }
        }

        public static void ResetTexteRVG()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PINFERVT", null);
            }
        }

        public static void ResetParameterAktenstatus()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("SDCCSTAT", null);
            }
        }

        public static void ResetForderungskonten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCACCT", null);
            }
        }

        public static void ResetParameterJobtypenZeitaufwand()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PTBJOBTI", null);
            }
        }

        public static void ResetUnfallangaben()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFALLS1", null);
            }
        }

        public static void ResetUnfallmassnahmen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFMASS", null);
            }
        }

        public static void ResetUnfallschadenDokumente()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFALLS2", null);
            }
        }

        public static void ResetUnfallschadenDokumenteInhalte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFALLS3", null);
            }
        }

        public static void ResetUnfallschadenBesichtigungsort()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFALLS4", null);
            }
        }

        public static void ResetUnfallschadenDaten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("UNFALLS1", null);
            }
        }

        public static void ResetAngabenAnwVS()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("ANWVS", null);
            }
        }

        public static void ResetAkteWebakte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("CMTOGDV", null);
            }
        }

        public static void ResetWebakteAdresse()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("GDVTOADR", null);
            }
        }

        public static void ResetSchadenerfassungAdresse()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("ADATOADR", null);
            }
        }

        public static void ResetRechnungWebakte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("RVGTOGDV", null);
            }
        }

        public static void ResetRechnungspositionenWebakte()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("RVGPOS", null);
            }
        }

        public static void ResetAdressatAdressat()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("AMADTOAD", null);
            }
        }

        public static void ResetAusgabewarteschlangen2()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCQUEUE2", null);
            }
        }

        public static void ResetAuszahlungFremdgeld()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DCPATRMO", null);
            }
        }

        public static void ResetBankverbindungen()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("AMBANK", null);
            }
        }

        public static void ResetMobileCases()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("EAMC", null);
                dataConnection.DeleteAndPackTable("EAMCC", null);
                dataConnection.DeleteAndPackTable("EAMCPB", null);
                dataConnection.DeleteAndPackTable("EAMCPBA", null);
            }
        }

        public static void ResetInformationDokumentenStatus()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DMSCHECK", null);
            }
        }

        public static void ResetParameterAusweisarten()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("PAMAUSWS", null);
            }
        }

        public static void ResetDocumentTags()
        {
            ResetDocumentTags(false);
        }

        public static void ResetDocumentTags(bool resetAllTagTables)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("DOCTAG", null);
                dataConnection.DeleteAndPackTable("DOCTAGL", null);

                if (resetAllTagTables)
                {
                    dataConnection.DeleteAndPackTable("DOCTAGT", null);
                    dataConnection.DeleteAndPackTable("DOCTAGCTL", null);
                    dataConnection.DeleteAndPackTable("DOCTAGC", null);
                }
            }
        }

        public static void ResetTeamDocs()
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("TEAMDOCSD", null);
                dataConnection.DeleteAndPackTable("TEAMDOCSDP", null);
            }
        }

        public static void ResetPermissions(bool reseed = false)
        {
            using (IDbConnection dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("PPERMTPL", reseed);
                dataConnection.DeleteAndPackTable("PPERMITE ", reseed);
                dataConnection.DeleteAndPackTable("PPERMGRP", reseed);
                dataConnection.DeleteAndPackTable("PPERMREL", reseed);
                dataConnection.DeleteAndPackTable("IPERMSTA", reseed);
                dataConnection.DeleteAndPackTable("IPERMREL", reseed);
            }
        }

        public static void ResetBEAMSG()
        {
            using (var dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("BEAMSG", true);
            }
        }

        public static void ResetAMAILIN()
        {
            using (var dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("AMAILIN", true);
            }
        }

        public static void ResetAMAILAIN()
        {
            using (var dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("AMAILAIN", true);
            }
        }

        public static void ResetBLBAMAILAIN()
        {
            using (var dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("BLBAMAILAIN", true);
            }
        }

        public static void ResetInboxServiceStatistics()
        {
            using (IDbConnection dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("DOCIMPSTATRUN", null);
                dataConnection.DeleteAndPackTable("DOCIMPSTATDOC", null);
            }
        }

        public static void ResetWorkingCopies()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("DHTODOC", null);
            }
        }

        public static void ResetWorkingCopiesContent()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("BLBDOCWORK", null);
            }
        }

        public static void ResetDocHeads()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("DOCHEAD", null);
            }
        }

        public static void ResetKeywords()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("DOCFIND", null);
                dataConnection.DeleteAndPackTable("DOCFTYPE", null);
            }
        }

        public static void ResetPdfEditorWorkspaces()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("PDFEWORK", null);
            }
        }

        public static void ResetUserFavorites()
        {
            using (var dataConnection = GetConnection())
            {
                dataConnection.Open();
                dataConnection.DeleteAndPackTable("TODOUSRFAV", null);
            }
        }

        public static void ResetKostenrecht()
        {
            using (IDbConnection dataConnection = GetConnectionATSystem())
            {
                dataConnection.Open();

                dataConnection.DeleteAndPackTable("KOSTRMOG", null);
            }
        }
    }
}