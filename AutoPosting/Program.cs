using System;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Configuration;





using System.Net;
using System.Net.Mail;

namespace AutoPosting
{
    class Program
    {
        public static void Email(string judgement, string emailrecipients, string emailcc, string emailcc2, bool periodclosing, string attachmentfilepath, DateTime startdate, DateTime enddate)
        {

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("amt_reporting@amt-mat.com");

            mail.To.Add(emailrecipients); //set email receipients here
            mail.CC.Add(emailcc);

            if (periodclosing)
            {
                mail.CC.Add(emailcc2);
            }

            SmtpClient SmtpServer = new SmtpClient();
            SmtpServer.Host = "smtp.office365.com";
            SmtpServer.Port = 587;
            SmtpServer.Credentials = new System.Net.NetworkCredential("amt_reporting@amt-mat.com", "Sos56395");
            SmtpServer.EnableSsl = true;

            try
            {
                if (judgement == "I") //Ignored 
                {
                    if (periodclosing)
                    {
                        mail.Subject = "Job AutoPosting Error - Ignored Jobs (Closing Period: " + startdate + " - " + enddate + ")";
                    }
                    else
                    {
                        mail.Subject = "Job AutoPosting Error - Ignored Jobs";
                    }

                    mail.Body = "Hi all, Attached file is a list of jobs ignored during autoposting with reasons. Please validate ASAP.";
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(attachmentfilepath);
                    mail.Attachments.Add(attachment);

                    SmtpServer.Send(mail);

                }
                else if (judgement == "U") //Unposted
                {
                    if (periodclosing)
                    {
                        mail.Subject = "Job AutoPosting Error - Unposted Jobs (Closing Period: " + startdate + " - " + enddate + ")";
                    }
                    else
                    {
                        mail.Subject = "Job AutoPosting Error - Unposted Jobs";
                    }

                    mail.Body = "Hi all, Attached file is a list of jobs unposted being held during autoposting. Please validate ASAP.";
                    System.Net.Mail.Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(attachmentfilepath);
                    mail.Attachments.Add(attachment);

                    SmtpServer.Send(mail);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }



        }
        static void Main(string[] args)
        {

            //this path is to read sql file and generate log files;
            string directoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            //for tls purpose, requires to set if .net version is below 4.7
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            String EmailSendTo = ConfigurationManager.AppSettings["EmailSendTo"];
            String EmailCC = ConfigurationManager.AppSettings["EmailCC"];
            String EmailCC2 = ConfigurationManager.AppSettings["EmailCC2"];

            //create a log file fo today and initialize a writer;
            string folderPath = @directoryPath + @"\log";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            //=======================================================Read Date==============================================================

            bool period_closing = false;
            bool read_date = false;
            bool send = false;
            DateTime todayDate = DateTime.Today, sd = DateTime.Today, ed = DateTime.Today;
            DateTime todayNow= DateTime.Now;
            DateTime todayMorningStart = DateTime.Today.AddHours(9), todayAfternoonStart = DateTime.Today.AddHours(14);
            DateTime todayMorningEnd = todayMorningStart.AddMinutes(15), todayAfternoonEnd = todayAfternoonStart.AddMinutes(15);

            if ((todayNow >= todayMorningStart && todayNow < todayMorningEnd)||(todayNow >= todayAfternoonStart && todayNow < todayAfternoonEnd)) // Between 9:00am to 9:15am and 2:00pm to 2:15pm, Send Email
            {
                send = true;
            }


            var start_dates = new List<string>();
            var end_dates = new List<string>();
            string excludeDates_str="";
            String datePath = ConfigurationManager.AppSettings["DateFilePath"];
            string datefilepath = @datePath;

            using (var reader = new StreamReader(datefilepath))
            {
                reader.ReadLine();
                read_date = true;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line != ",")
                    {

                        var values = line.Split(',');
                        start_dates.Add(values[0]);
                        end_dates.Add(values[1]);
                    }
                }
                Console.WriteLine(start_dates + "_" + end_dates);
            }


            for (int i = 0; i < start_dates.Count; i++)
            {

                sd = DateTime.ParseExact(start_dates[i], "d/M/yyyy", CultureInfo.InvariantCulture);
                ed = DateTime.ParseExact(end_dates[i], "d/M/yyyy", CultureInfo.InvariantCulture);

                // add ed to 1 more day 
                ed = ed.AddDays(1);


                if (todayDate >= sd && todayDate < ed)
                {
                    period_closing = true;

                    //convert datetime to excel datetime format
                    var s = sd.ToOADate();
                    excludeDates_str = s.ToString();

                    //breaks when todayDate is between sd and ed
                    break;
                }
            }

            //=======================================================End Read Date==============================================================

            //=======================================================Open Filepath==============================================================
            string filePath = @directoryPath + @"\log\log_all\autoposting_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt";
            StreamWriter writer = File.AppendText(Path.Combine(folderPath, filePath));
            writer.Write("==================" + DateTime.Now.ToString("HH:mm:ss") + "==================");

            string csvUnpostedPath = @directoryPath + @"\log\log_unposted\unpostedjobs_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm") + ".csv";
            string csvIgnoredPath = @directoryPath + @"\log\log_ignored\ignoredjobs_" + DateTime.Now.ToString("dd-MM-yyyy_HH-mm") + ".csv";

            if (!File.Exists(csvUnpostedPath))
            {
                using (StreamWriter csvUnpostedCreate = File.AppendText(csvUnpostedPath))
                {
                    csvUnpostedCreate.WriteLine("job,suffix,oper_num,qty_complete,qty_scrapped");
                    csvUnpostedCreate.Flush();
                    csvUnpostedCreate.Close();
                }
            }

            if (!File.Exists(csvIgnoredPath))
            {
                using (StreamWriter csvIgnoredCreate = File.AppendText(csvIgnoredPath))
                {
                    csvIgnoredCreate.WriteLine("Job,Oper,CSI complete qty,CSI scrapped qty,Ewip complete qty,Ewip scrapped qty,Remarks");
                    csvIgnoredCreate.Flush();
                    csvIgnoredCreate.Close();
                }
            }

            StreamWriter csvUnpostedWriter = File.AppendText(csvUnpostedPath);
            StreamWriter csvIgnoredWriter = File.AppendText(csvIgnoredPath);

            //=======================================================End Open File Path==============================================================


            //==========variables used===========
            String sessionToken;
            SqlConnection cnn;
            SqlCommand command;
            SqlDataAdapter da;
            DataTable dt = new DataTable();
            DataTable check_dt = new DataTable();
            DataTable check_dt_lot = new DataTable();
            DataTable postnumdt = new DataTable();
            DataTable anomaly = new DataTable();
            DataTable check_qty_released = new DataTable();
            int num_unposted = 0;
            DataSet idoDS, lastDS, lotDS;
            DataTable idoTB, lastTB, lotTB;
            String sql_script = null, ewiptbname, amttbname, amtjttbname, amtjobtbname, anomaly_trans;
            String unposted_writeline, ignored_writeline;
            int tablenum = 1, ignorerowsnum_qtyexceed = 0, ignorerowsnum_runratenotequal = 0, ignorerowsnum_last = 0, ignorerowsnum_a_hrs = 0, ignorerownum_duplicate = 0, linesignored = 0, linesunposted = 0;
            int lotExist = 0;
            bool oper_reversal = false, a_hrs_reversal=false, transmain_dupe = false, qty_exceed_release = false, run_rate_not_equal = false;
            int startup = 0, judge = 0, judgecount = 0, oneloop = 0;
            String sqlfilename = "";

            //variables can be modified from config file
            String part = ConfigurationManager.AppSettings["DataCollect"].ToLower();
            String connectionstr = ConfigurationManager.AppSettings["SqlConnection"];
            String CSIUser = ConfigurationManager.AppSettings["CSI_UserName"];
            String CSIPass = ConfigurationManager.AppSettings["CSI_Password"];
            String CSIDB = ConfigurationManager.AppSettings["CSI_DataBase"];
            String prefix = ConfigurationManager.AppSettings["Job Prefix Not In"];
            String tooling = ConfigurationManager.AppSettings["Tooling"].ToLower();

            switch (part)
            {
                case "amtlive":
                    sqlfilename = "Filter.sql";
                    ewiptbname = "[eWIP_AMT].[dbo].[tbJobTransMain]";
                    amttbname = "[AMTLive].[dbo].[lot_mst]";
                    amtjttbname = "[AMTLive].[dbo].[jobtran_mst]";
                    amtjobtbname = "[AMTLive].[dbo].[job_mst]";
                    break;
                case "yixinlive":
                    sqlfilename = "Filter_YX.sql";
                    ewiptbname = "[YXERPSVR].[eWIP_YX].[dbo].[tbJobTransMain]";
                    amttbname = "[YixinLive].[dbo].[lot_mst]";
                    amtjttbname = "[YixinLive].[dbo].[jobtran_mst]";
                    amtjobtbname = "[YixinLive].[dbo].[job_mst]";
                    break;
                case "yixintest":
                    sqlfilename = "Filter_test_YX.sql";
                    ewiptbname = "[YXERPSVR].[eWIP_YX_Test].[dbo].[tbJobTransMain]";
                    amttbname = "[YixinTest].[dbo].[lot_mst]";
                    amtjttbname = "[YixinTest].[dbo].[jobtran_mst]";
                    amtjobtbname = "[YixinTest].[dbo].[job_mst]";
                    break;
                default: //amttest
                    sqlfilename = "Filter_test.sql";
                    ewiptbname = "[eWIP_Test].[dbo].[tbJobTransMain]";
                    amttbname = "[AMTTest].[dbo].[lot_mst]";
                    amtjttbname = "[AMTTest].[dbo].[jobtran_mst]";
                    amtjobtbname = "[AMTTest].[dbo].[job_mst]";
                    break;
            }

            String transtype, reasoncode;
            Decimal post_complete, post_scrapped, pr_job_rate, main_comp, erp_comp, main_scra, erp_scra, qty_rel, a_hrs = 0, a_hrs_ewip = 0;//runrate_temp, 
            int nextoper = 0;
            DateTime purgedate = DateTime.Today, expdate = DateTime.Today;

            //posting parameter initialization//
            /*If need change posting settings, just change pa*/

            string pa_common = "<Parameters>" +
                "<Parameter>0</Parameter>" +
                "<Parameter>0</Parameter>" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter>0000</Parameter>" +
                "<Parameter>9999</Parameter>" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter>MES</Parameter>" +
                "<Parameter>MES</Parameter>" +
                "<Parameter>H S N</Parameter>" +
                "<Parameter>MAIN</Parameter>" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "<Parameter />" +
                "</Parameters>";

            string pa_complete = "<Parameters>" +
                "<Parameter>0</Parameter>" +    //V(PostCompleteVar)  //complete job, either 0 or 1 
                "<Parameter>0</Parameter>" +    //V(PostNegativeInventoryVar)
                "<Parameter />" +               //V(StartJobVar)
                "<Parameter />" +               //V(EndJobVar)
                "<Parameter>0000</Parameter>" + //V(StartSuffixVar)
                "<Parameter>9999</Parameter>" + //V(EndSuffixVar)
                "<Parameter />" +               //V(StartTransDateVar)
                "<Parameter />" +               //V(EndTransDateVar)
                "<Parameter />" +               //V(StartEmpNumVar)
                "<Parameter />" +               //V(EndEmpNumVar)
                "<Parameter />" +               //(StartDeptVar)
                "<Parameter />" +               //V(EndDeptVar)
                "<Parameter />" +               //V(StartShiftVar)
                "<Parameter />" +               //V(EndShiftVar)
                "<Parameter>MES</Parameter>" +  //V(StartUserCodeVar)
                "<Parameter>MES</Parameter>" +  //V(EndUserCodeVar)
                "<Parameter>H S N</Parameter>" +//V(HourlyVar) V(SalaryVar) V(NonEmployeeVar)
                "<Parameter>MAIN</Parameter>" + //V(FormCurWhse)
                "<Parameter />" +               //V()
                "<Parameter />" +               //V()
                "<Parameter />" +               //RVAR V(PromptMessage)
                "<Parameter />" +               //RVAR V(PromptButtons)
                "<Parameter />" +
                "<Parameter />" +
                "</Parameters>";


            //======create a dataset for saving data, initialize all columns; 17 columns======
            idoDS = new DataSet("SLJobTrans");      //Table 1 in the first dataset;
            idoTB = idoDS.Tables.Add("IDO");
            idoTB.Columns.Add("TransType", typeof(string));
            idoTB.Columns.Add("TransDate", typeof(DateTime));
            idoTB.Columns.Add("Job", typeof(string));
            idoTB.Columns.Add("OperNum", typeof(int));
            idoTB.Columns.Add("UserCode", typeof(string));
            idoTB.Columns.Add("EmpNum", typeof(string));
            idoTB.Columns.Add("Shift", typeof(int));
            idoTB.Columns.Add("PayRate", typeof(string));
            idoTB.Columns.Add("PrRate", typeof(decimal));
            idoTB.Columns.Add("JobRate", typeof(decimal));
            idoTB.Columns.Add("QtyComplete", typeof(decimal));
            idoTB.Columns.Add("QtyScrapped", typeof(decimal));
            idoTB.Columns.Add("QtyMoved", typeof(decimal));
            idoTB.Columns.Add("Whse", typeof(string));
            idoTB.Columns.Add("NextOper", typeof(int));
            idoTB.Columns.Add("AHrs", typeof(decimal));
            idoTB.Columns.Add("ReasonCode", typeof(string));

            //======create another table for the last oper, initialize all columns. 21 columns======
            lastDS = new DataSet("SLJobTrans");      //Table 2 in the second dataset;
            lastTB = lastDS.Tables.Add("IDO");
            lastTB.Columns.Add("TransType", typeof(string));
            lastTB.Columns.Add("TransDate", typeof(DateTime));
            lastTB.Columns.Add("Job", typeof(string));
            lastTB.Columns.Add("OperNum", typeof(int));
            lastTB.Columns.Add("UserCode", typeof(string));
            lastTB.Columns.Add("EmpNum", typeof(string));
            lastTB.Columns.Add("Shift", typeof(int));
            lastTB.Columns.Add("PayRate", typeof(string));
            lastTB.Columns.Add("PrRate", typeof(decimal));
            lastTB.Columns.Add("JobRate", typeof(decimal));
            lastTB.Columns.Add("QtyComplete", typeof(decimal));
            lastTB.Columns.Add("QtyScrapped", typeof(decimal));
            lastTB.Columns.Add("QtyMoved", typeof(decimal));
            lastTB.Columns.Add("Whse", typeof(string));
            lastTB.Columns.Add("AHrs", typeof(decimal));
            lastTB.Columns.Add("ReasonCode", typeof(string));
            lastTB.Columns.Add("Loc", typeof(string));                  //only for last oper;
            lastTB.Columns.Add("CloseJob", typeof(byte));               //only for last oper;
            lastTB.Columns.Add("Lot", typeof(string));                  //only for last oper;
            lastTB.Columns.Add("Uf_ExpirationDate", typeof(DateTime));  //only for last oper;
            lastTB.Columns.Add("LotManufacturedDate", typeof(DateTime));//only for last oper;

            //create a table for inserting in lot_mst, initialize all columns;
            lotDS = new DataSet("SLLots");          //Table 3 for lot_mst
            lotTB = lotDS.Tables.Add("IDO");
            lotTB.Columns.Add("Lot", typeof(string));
            lotTB.Columns.Add("Item", typeof(string));
            lotTB.Columns.Add("Revision", typeof(string));
            lotTB.Columns.Add("PurgeDate", typeof(DateTime));
            lotTB.Columns.Add("ExpDate", typeof(DateTime));
            lotTB.Columns.Add("CreateDate", typeof(DateTime)); //Added CreateDate 27/7/23
            lotTB.Columns.Add("ManufacturedDate", typeof(DateTime));
            lotTB.Columns.Add("RcvdQty", typeof(decimal));

            //==========Data collect & connect to SQL database;==========
            //connectionstr = @"Data Source=AMTERP03;User ID=tester;Password=qwer1234";
            cnn = new SqlConnection(@connectionstr);
            sql_script = File.ReadAllText(@directoryPath + @"\" + @sqlfilename);   //put the sql file path here;

            //type of Work Orders - Tooling/Machining etc..
            if (tooling == "tooling") 
            {
                sql_script = sql_script + " and ([jobroute_mst].[qty_complete] <> [tbJobTransMain].[_qty_complete] or [jobroute_mst].[qty_scrapped] <> [tbJobTransMain].[_qty_scrapped] or (([jobroute_mst].run_hrs_t_mch+[jobroute_mst].run_hrs_t_lbr)<>ROUND([tbJobTransMain].a_hrs / 60,3)))";
                sql_script = sql_script + " and [jobroute_mst].oper_num <> 100"; //exclude Issue Raw Materials
                sql_script = sql_script + " and [job_mst].item like 'YT%'";
            }
            else if (tooling == "machining")
            {
                sql_script = sql_script + " and ([jobroute_mst].[qty_complete] <> [tbJobTransMain].[_qty_complete] or [jobroute_mst].[qty_scrapped] <> [tbJobTransMain].[_qty_scrapped])";
                sql_script = sql_script + " and [job_mst].item like 'YC%'";
            }
            else // not tooling
            {
                sql_script = sql_script + " and ([jobroute_mst].[qty_complete] <> [tbJobTransMain].[_qty_complete] or [jobroute_mst].[qty_scrapped] <> [tbJobTransMain].[_qty_scrapped])";
                sql_script = sql_script + " and [job_mst].item not like 'YT%'";
            }

            if (prefix == "''" && !period_closing) //live - not within period closing
            {
                sql_script = sql_script + " ";
            }
            else if (prefix == "''" && period_closing) //live - within period closing, excludes trans before start of closing period
            {
                sql_script = sql_script + " and [tbJobTransMain]._end_Date < " + @excludeDates_str;
            }
            else //not live - trigger this by adding random char in prefix at app.config
            {
                sql_script = sql_script + "	and [job_mst].job = 'YC06230007'";
            }

            sql_script += " Order by [job_mst].job desc, [jobroute_mst].[oper_num]";

            cnn.Open();
            command = new SqlCommand(sql_script, cnn);
            da = new SqlDataAdapter(command);
            da.Fill(dt);

            writer.WriteLine("          total num of rows: " + dt.Rows.Count.ToString());   //number of records in this run;
            writer.WriteLine();

            SqlDataAdapter check_adapter = new SqlDataAdapter();
            //==========End collect in dt==========

            judge = dt.Rows.Count;

            while (oneloop == 0)
            {
                if (judge < 100) //batch cycle
                {
                    judgecount = judge;
                    oneloop = 1;
                }
                else
                {
                    judgecount = 100; //batch cycle
                }

                //data cleaning(in auto run loop)
                for (int j = 0; j < judgecount; j++)
                {
                    var row = dt.Rows[j + startup];

                    main_comp = decimal.Parse(row[15].ToString());
                    erp_comp = decimal.Parse(row[14].ToString());
                    main_scra = decimal.Parse(row[16].ToString());
                    erp_scra = decimal.Parse(row[11].ToString());
                    post_complete = main_comp - erp_comp;   //Complete Qty;
                    post_scrapped = main_scra - erp_scra;   //Scrapped Qty;

                    //check if main_comp and main_scra does not exceed job_mst's qty_released
                    check_qty_released.Clear();
                    command = new SqlCommand($"SELECT* From {amtjobtbname} Where job=@Job", cnn);
                    command.Parameters.AddWithValue("@Job", row[0]);
                    check_adapter = new SqlDataAdapter(command);
                    check_adapter.Fill(check_qty_released);
                    qty_rel = decimal.Parse(check_qty_released.Rows[0][13].ToString());


                    //Tooling a_hrs
                    if (tooling == "tooling")
                    {
                        a_hrs_ewip = decimal.Parse(row[41].ToString()); //a_hrs_EWIP
                        if (row[40].ToString() != "") //if a_hrs_CSI is not null
                        {
                            a_hrs = decimal.Parse(row[40].ToString()); //a_hrs_CSI
                        }

                        a_hrs = a_hrs_ewip - a_hrs;

                        if (row[18].ToString() == "L")          //For labour    //sched_drv
                        {
                            transtype = "R";

                            pr_job_rate = decimal.Parse(row[31].ToString());    //prrate & jobrate //run_rate_lbr

                            //a_hrs = decimal.Parse(row[20].ToString()) * post_complete; //run_lbr_hrs

                            //if ((a_hrs < 0) && (Math.Abs(a_hrs) > decimal.Parse(row[38].ToString()))) //run_hrs_t_lbr
                            //{
                            //    a_hrs = decimal.Parse(row[38].ToString()); //run_hrs_t_lbr
                            //}

                        }
                        else                                    //For machine
                        {
                            transtype = "C";

                            pr_job_rate = 0;

                            //a_hrs = decimal.Parse(row[19].ToString()) * post_complete; //run_mch_hrs

                            //if ((a_hrs < 0) && (Math.Abs(a_hrs) > decimal.Parse(row[37].ToString()))) //run_hrs_t_mch 
                            //{
                            //    a_hrs = decimal.Parse(row[37].ToString());
                            //}

                        }

                    }
                    else
                    {
                        if (row[18].ToString() == "L")          //For labour    //sched_drv
                        {
                            transtype = "R";

                            pr_job_rate = decimal.Parse(row[31].ToString());    //prrate & jobrate //run_rate_lbr

                            a_hrs = decimal.Parse(row[20].ToString()) * post_complete; //run_lbr_hrs

                            if ((a_hrs < 0) && (Math.Abs(a_hrs) > decimal.Parse(row[38].ToString()))) //run_hrs_t_lbr
                            {
                                a_hrs = decimal.Parse(row[38].ToString()); //run_hrs_t_lbr
                            }

                        }
                        else                                    //For machine
                        {
                            transtype = "C";

                            pr_job_rate = 0;

                            a_hrs = decimal.Parse(row[19].ToString()) * post_complete; //run_mch_hrs

                            if ((a_hrs < 0) && (Math.Abs(a_hrs) > decimal.Parse(row[37].ToString()))) //run_hrs_t_mch 
                            {
                                a_hrs = decimal.Parse(row[37].ToString());
                            }

                        }
                    }

                    //ignored jobs
                    if (main_comp + main_scra > qty_rel)      //if main_comp and main_scra exceeds job_mst's qty_released
                    {
                        tablenum = 0;
                        qty_exceed_release = true;
                    }

                    //if (post_complete < 0 && row[18].ToString() == "L")   // For run rate validation 
                    //{
                    //    if (a_hrs == 0) //to solve divided by 0 issue 
                    //    {
                    //        runrate_temp = 0;
                    //    }
                    //    else
                    //    {
                    //        runrate_temp = decimal.Parse(row[39].ToString()) / a_hrs;
                    //    }

                    //    if (runrate_temp != pr_job_rate) //validate job rate is the same
                    //    {
                    //        tablenum = 0;
                    //        run_rate_not_equal = true;
                    //    }

                    //}
                    
                    if (post_complete < 0) //if at last step the completeqty < 0, just ignore this line;
                    {
                        tablenum = 0;
                        oper_reversal = true;
                    }

                    if (a_hrs_ewip < 0) //negative a_hrs
                    {
                        tablenum = 0;
                        a_hrs_reversal = true;
                    }

                    if (main_scra < erp_scra)                //ReasonCode
                    {
                        reasoncode = "RW";
                    }
                    else if (main_scra > 0)
                    {
                        reasoncode = "PF";
                    }
                    else
                    {
                        reasoncode = null;
                    }

                    if (row[6].Equals(row[25]))     //last oper
                    {
                        tablenum = 2;

                        //--------------------expiration_date & purge_date--------------------
                        if (row[26].ToString() == "FIFO")
                        {
                            if (row[30] == DBNull.Value)
                            {
                                row[30] = 0;
                            }

                            if (Convert.ToInt32(row[30]) == 0)
                            {
                                expdate = DateTime.Today.AddYears(10);  //FIFO & shelf_life = 0;
                            }
                            else
                            {
                                expdate = DateTime.Today.AddMonths(int.Parse(row[30].ToString()));
                            }

                            purgedate = expdate;
                        }
                        else
                        {
                            expdate = DateTime.Today.AddYears(3);       //FEFO;

                            purgedate = expdate.AddMonths(-int.Parse(row[30].ToString()));
                        }
                        //--------------------end date_related value--------------------


                    }
                    else
                    {
                        nextoper = Convert.ToInt32(row[27]);
                    }

                    //==========check mutiple job order case in tbJobTransMain==========
                    command = new SqlCommand($"SELECT* From {ewiptbname} Where _job = @Job and _oper_num = @Opernum", cnn);
                    command.Parameters.AddWithValue("@Job", row[0]);
                    command.Parameters.AddWithValue("@Opernum", row[6]);
                    check_adapter = new SqlDataAdapter(command);
                    check_adapter.Fill(check_dt);

                    if (check_dt.Rows.Count > 1)        //To check duplicate rows in jobtransmain;
                    {
                        tablenum = 0;                   //skip save data;
                        transmain_dupe = true;

                    }

                    //==========check if exist lot_mst==========
                    command = new SqlCommand($"SELECT* From {amttbname} Where item = @Item and lot = @Lot", cnn);
                    command.Parameters.AddWithValue("@Item", row[4]);
                    command.Parameters.AddWithValue("@Lot", row[0]);
                    check_adapter = new SqlDataAdapter(command);
                    check_adapter.Fill(check_dt_lot);

                    if (check_dt_lot.Rows.Count > 0)
                    {
                        lotExist = 1;           //skip save data; Add count for this part;
                    }

                    //========================Save each data line under different conditions========================
                    if (tablenum == 1)
                    {
                        //This table collect job orders which is not in the last opernum;
                        idoTB.Rows.Add(new object[] { transtype, DateTime.Today, row[0], row[6], "MES", "PROD", 0, "R", pr_job_rate, pr_job_rate, post_complete, post_scrapped, post_complete, "MAIN", nextoper, a_hrs, reasoncode });
                    }
                    else if (tablenum == 2)
                    {
                        //This table collect job orders which is in the last opernum;
                        lastTB.Rows.Add(new object[] { transtype, DateTime.Today, row[0], row[6], "MES", "STORE", 0, "R", pr_job_rate, pr_job_rate, post_complete, post_scrapped, post_complete, "MAIN", a_hrs, reasoncode, "TL", 0, row[0], expdate, DateTime.Today });

                        //This table collect lot;
                        if (lotExist == 0)
                        {
                            lotTB.Rows.Add(new object[] { row[0], row[4], row[36], purgedate, expdate, DateTime.Today, DateTime.Today, post_complete}); //Added CreateDate 27/7/23
                        }
                    }
                    else                    //==========export ignored lines==========
                    {

                        if (tablenum == 0)
                        {
                            if (oper_reversal)
                            {
                                ignored_writeline = row[0] + "," + row[6] + "," + erp_comp + "," + erp_scra + "," + main_comp + "," + main_scra + "," + "last oper reversal";
                                ignorerowsnum_last++;                       //For last oper ignored lines.
                                csvIgnoredWriter.WriteLine(ignored_writeline);
                            }

                            if (a_hrs_reversal)
                            {
                                ignored_writeline = row[0] + "," + row[6] + "," + erp_comp + "," + erp_scra + "," + main_comp + "," + main_scra + "," + "negative cycle time";
                                ignorerowsnum_a_hrs++;                       //For last oper ignored lines.
                                csvIgnoredWriter.WriteLine(ignored_writeline);
                            }

                            if (transmain_dupe)
                            {
                                ignored_writeline = row[0] + "," + row[6] + ",,,,,duplicate job transMain";
                                ignorerownum_duplicate++;       //For duplicate ignored jobs;
                                csvIgnoredWriter.WriteLine(ignored_writeline);
                            }

                            if(qty_exceed_release)
                            {
                                ignored_writeline = row[0] + "," + row[6] + ",,,,,qty complete and qty scrap exceeds qty released";
                                ignorerowsnum_qtyexceed++;                       //For qty exceeded ignored lines.
                                csvIgnoredWriter.WriteLine(ignored_writeline);
                            }

                            if(run_rate_not_equal)
                            {
                                ignored_writeline = row[0] + "," + row[6] + ",,,,,run rate not equal to intended run rate";
                                ignorerowsnum_runratenotequal++;                       //For qty current run rate not equal to intended run rate.
                                csvIgnoredWriter.WriteLine(ignored_writeline);
                            }
                            linesignored++;
                        }
                    }
                    //====================================End save====================================
                    oper_reversal = false;
                    transmain_dupe = false;
                    qty_exceed_release = false;
                    run_rate_not_equal = false;

                    tablenum = 1;

                    lotExist = 0;

                    check_dt.Clear();

                    check_dt_lot.Clear();

                }

                /*SaveDataSet action will skip the empty dataset, so no need to check if they are null*/
                /*there is some settings in App.config, cuz the messagesize by default is too small, will return error*/

                //====================================save dataset to unposted====================================

                if(!period_closing)
                {
                    IDOWebService.DOWebServiceSoapClient idoWS = new IDOWebService.DOWebServiceSoapClient();//initialize

                    idoWS.Open();

                    sessionToken = idoWS.CreateSessionToken(CSIUser, CSIPass, CSIDB);

                    try
                    {
                        idoWS.SaveDataSet(sessionToken, idoDS, true, "", "", "");     //save dataset 1;
                        writer.WriteLine("Save 'SLJobTrans' successfully!(not last oper) " + idoTB.Rows.Count + " inserted.");
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Save 'SLJobTrans' failed!(not last oper)" + ex.Message);
                    }

                    try
                    {
                        idoWS.SaveDataSet(sessionToken, lastDS, true, "", "", "");    //save dataset 2;
                        writer.WriteLine("Save 'SLJobTrans' successfully!(last oper)     " + lastTB.Rows.Count + " inserted.");
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Save 'SLJobTrans' failed!(last oper)" + ex.Message);
                    }

                    try
                    {
                        idoWS.SaveDataSet(sessionToken, lotDS, true, "", "", "");     //save lot dataset;
                        writer.WriteLine("[Save   'SLLots'   successfully!                " + lotTB.Rows.Count + " inserted.]");
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Save   'SLLots'   failed!" + ex.Message);
                    }

                    writer.WriteLine();
                    //====================================END save dataset to unposted====================================

                    //====================================posting====================================

                    try
                    {
                        postnumdt.Clear();
                        command = new SqlCommand($"SELECT* From {amtjttbname} Where posted = 0 and user_code ='MES'", cnn);
                        check_adapter = new SqlDataAdapter(command);
                        check_adapter.Fill(postnumdt);

                        num_unposted = postnumdt.Rows.Count; //Perform a unposted row count before posting

                        while (postnumdt.Rows.Count > 0)
                        {
                            idoWS.CallMethod(sessionToken, "SL.SLJobTrans", "JobJobP", ref pa_common);          //post not complete orders
                            idoWS.CallMethod(sessionToken, "SL.SLJobTrans", "JobJobP", ref pa_complete);        //post complete orders;

                            postnumdt.Clear();
                            command = new SqlCommand($"SELECT* From {amtjttbname} Where posted = 0 and user_code ='MES'", cnn);
                            check_adapter = new SqlDataAdapter(command);
                            check_adapter.Fill(postnumdt);

                            if (postnumdt.Rows.Count > 0)
                            {
                                //====================================remove anomalies====================================
                                anomaly.Clear();
                                command = new SqlCommand($"SELECT top(1)* From {amtjttbname} Where posted = 0 and user_code ='MES' order by job, oper_num", cnn);
                                check_adapter = new SqlDataAdapter(command);
                                check_adapter.Fill(anomaly);

                                anomaly_trans = anomaly.Rows[0][1].ToString();

                                //fill in unposted exception
                                foreach (DataRow row in anomaly.Rows)
                                {
                                    unposted_writeline = row[2] + "," + row[3] + "," + row[8] + "," + row[6] + "," + row[7];
                                    csvUnpostedWriter.WriteLine(unposted_writeline);
                                    linesunposted++;

                                }

                                //delete that one job that failed to post
                                anomaly.Clear();
                                command = new SqlCommand($"DELETE From {amtjttbname} Where posted = 0 and user_code ='MES' and trans_num=@Trans", cnn);
                                command.Parameters.AddWithValue("@Trans", anomaly_trans);
                                check_adapter = new SqlDataAdapter(command);
                                check_adapter.Fill(anomaly);

                                //====================================end remove anomalies====================================
                            }

                        }

                        writer.WriteLine("Post successfully!                             " + (num_unposted - linesunposted) + " affected.");

                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine("Post failed!" + ex.Message);
                    }



                    idoWS.Close();
                    //====================================END posting====================================
                }


                writer.WriteLine();

                startup = startup + 100; //batch cycle

                judge = judge - 100; //batch cycle

                idoDS.Clear();

                lastDS.Clear();

                lotDS.Clear();

                postnumdt.Clear();

            }


            csvUnpostedWriter.Flush();
            csvUnpostedWriter.Close();
            csvIgnoredWriter.Flush();
            csvIgnoredWriter.Close();

            //Testing
            //period_closing = true;
            //send = true;

            //========================================================Send Email=========================================================================================
            if (linesunposted > 0 && send) //generate unposted log file if lines unposted exist and is around 9am or 2pm
            {
                Email("U", @EmailSendTo, @EmailCC, @EmailCC2, @period_closing, @csvUnpostedPath, @sd, @ed);             //call the email function to deal with special cases;
            }

            if (linesignored > 0 && send) //generate ignored log file if lines ignored exist and is around 9am or 2pm
            {
                Email("I", @EmailSendTo, @EmailCC, @EmailCC2, @period_closing, @csvIgnoredPath, @sd, @ed);             //call the email function to deal with special cases;
            }


            //========================================================End Send Email=========================================================================================

            writer.WriteLine("Ignored completed + rejected quantity exceeded jobs: " + ignorerowsnum_qtyexceed);
            writer.WriteLine("Ignored last oper jobs: " + ignorerowsnum_last);
            writer.WriteLine("Ignored duplicate jobs: " + ignorerownum_duplicate);
            //writer.WriteLine("Ignored different runrate jobs: " + ignorerowsnum_runratenotequal);
            writer.WriteLine();

            writer.WriteLine("Total Ignored jobs: " + linesignored);
            writer.WriteLine("Total Failed Unposted jobs: " + linesunposted);
            writer.WriteLine();

            writer.WriteLine("Read Date:" + read_date);
            writer.WriteLine("Closing Period: " + period_closing);
            writer.WriteLine("Send Email:" + send);
            writer.WriteLine();

            writer.Flush();
            writer.Close();
            command.Dispose();
            cnn.Close();
            dt.Dispose();
            check_dt.Dispose();
            check_dt_lot.Dispose();
            check_qty_released.Dispose();
            postnumdt.Clear();
            postnumdt.Dispose();
            anomaly.Dispose();
            idoDS.Dispose();
            lastDS.Dispose();
            lotDS.Dispose();

        }
    }
}


