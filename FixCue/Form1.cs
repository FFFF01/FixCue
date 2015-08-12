using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.Security.Principal;



namespace WindowsApplication2
{ 
    public partial class Form1 : Form
    {
        
        Regex re = new Regex(@"(.+)\\(?<filename>.+)\.(.+)");
        Regex fileline = new Regex(@"FILE ""(.+)\.([^.]+?)""", RegexOptions.IgnoreCase);
        string original_path = ""; //ԭʼ�ļ���       
        string readText = "";   //����
       // int code = 0;  //����
        string FileInFileLine;        
        INIClass config = new INIClass(Application.StartupPath+@"\config.ini");
        List<string> musicfiles = new List<string>();
        char[] JISMapBuffer = File.ReadAllText(Application.StartupPath + @"\maps\jis.map", Encoding.Unicode).ToCharArray();
        char[] GBMapBuffer = File.ReadAllText(Application.StartupPath + @"\maps\gb.map", Encoding.Unicode).ToCharArray();
        char[] Big5MapBuffer = File.ReadAllText(Application.StartupPath + @"\maps\big5.map", Encoding.Unicode).ToCharArray();
        bool loaded = false;
        bool forcecode = false;
        //bool started = false;

        public Form1()
        {
            InitializeComponent();
        }

        private string OpenFile(string original_path)
        {
            if (forcecode)
            {
                if (comboBox1.Text == "950")
                    ReadBIG5();
                else readText = File.ReadAllText(original_path, Encoding.GetEncoding(int.Parse(comboBox1.Text)));
                forcecode = false;
            }
            else
            {
                switch (getcodetype(original_path))
                {
                    case "CODETYPE_UTF8NOBOM":
                        {
                            var utf8WithoutBom = new UTF8Encoding(false);
                            readText = File.ReadAllText(original_path, utf8WithoutBom);                            
                            break;
                        }
                    case "CODETYPE_SHIFTJIS":
                        {
                            readText = File.ReadAllText(original_path, Encoding.GetEncoding(932));
                            break;
                        }
                    case "CODETYPE_DEFAULT":
                        {
                            if (String.IsNullOrEmpty(comboBox1.Text))
                            {
                                comboBox1.Text = "932";
                            }
                            if (comboBox1.Text == "950")
                                ReadBIG5();
                            else readText = File.ReadAllText(original_path, Encoding.GetEncoding(int.Parse(comboBox1.Text)));
                            break;
                        }
                    case "CODETYPE_GBK":
                        {
                            readText = File.ReadAllText(original_path, Encoding.GetEncoding(936));
                            break;
                        }
                    case "CODETYPE_BIG5":
                        {
                            ReadBIG5();
                            break;
                        }
                }
            }
            string FileInFileLine = fileline.Match(readText).Groups[1].Value.ToLower();
            return FileInFileLine;
        }        

        private void ReadBIG5()
        {
            readText = "";
            // readText = File.ReadAllText(original_path, Encoding.GetEncoding(950));
            //char[] Big5MapBuffer = File.ReadAllText(Application.StartupPath + @"\maps\big5.map", Encoding.Unicode).ToCharArray();
            Byte[] MyByte = File.ReadAllBytes(original_path);
            int high, low, chr, i;
            //   var OutPutByte = new List<Byte>();
            for (i = 0; i < MyByte.Length; )
            {
                high = MyByte[i]; //��ȡ��һ��byte
                i++;
                if (high > 0x7F) //��һ��byte�Ǹ�λ
                {
                    low = MyByte[i]; //��ȡ��λ
                    i++;
                }
                else
                {
                    low = high;
                    high = 0;
                }
                chr = low + high * 256;
                if (chr < 0x80) // ASCII��
                {
                    var encoding = new UnicodeEncoding();
                    readText += encoding.GetString(new byte[] { (byte)chr, 0 });
                }
                else
                {
                    char a = Big5MapBuffer[chr - 0x8140];
                    readText += a;
                }
            }
        }

        private void ChangeOutputFilePath()
        {
            FilePathTextBox.Text = re.Replace(original_path, @"$1\" + RenameTextBox.Text.Replace(@"%filename%", @"${filename}") + @".$2");
        }

        private bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }

        private void main_func()   // ��Ҫת�벿��
        {
            loaded = true;
            if (!AutoOutputCheckBox.Checked)
                OutputButton.Enabled = true;

            if (!CoverOldFileCheckBox.Checked)
                ChangeOutputFilePath();
            else
                FilePathTextBox.Text = original_path;

            FileInFileLine = OpenFile(original_path);
            string[] exts = { "ape", "tta", "flac", "tak", "wav", "m4a","wv" };
            musicfiles.Clear();    

            if (Path.GetExtension(original_path).ToLower() == ".cue")
            {
                // �Զ�������The True Audio������WAVE���Ա���fb2kʶ��
                if (ttafixCheckBox.Checked)
                {
                    readText = Regex.Replace(readText, "the true audio", "WAVE", RegexOptions.IgnoreCase);
                }

                foreach (string ext in exts)
                {
                    musicfiles.AddRange(Directory.GetFiles(Path.GetDirectoryName(original_path), "*." + ext));
                }
            
                bool MultiMusicFileType = false;

                // �Զ��滻FILE�ֶ��ļ���
                if (fileLineCheckBox.Checked)
                {
                    if (musicfiles.Count == 0)  //���û���ѵ����������ļ��������κ�����
                    { }
                    else if (musicfiles.Count == 1)  //���ֻ�ѵ�һ������ô��FILE�еı������ļ��滻
                    {
                        string MyFile = Path.GetFileName(musicfiles[0]);
                        readText = fileline.Replace(readText, @"FILE """ + MyFile + @"""");
                    }
                    else  //����ѵ����������������Ƿ���FileInFileLineƥ��
                    {
                        string MyExt = Path.GetExtension(musicfiles[0]); 
                        for (int i = 1; i < musicfiles.Count; i++)
                        {
                            if (Path.GetExtension(musicfiles[i]) != MyExt)
                            {
                                MultiMusicFileType = true;
                                break;
                            }
                        } 
                            foreach (string MyFilePath in musicfiles)
                            {
                                if (FileInFileLine == Path.GetFileNameWithoutExtension(MyFilePath).ToLower())
                                {
                                    string MyFile = Path.GetFileName(MyFilePath);
                                    readText = fileline.Replace(readText, @"FILE """ + MyFile + @"""");
                                    break;
                                }
                                if (Path.GetFileNameWithoutExtension(original_path).ToLower() == Path.GetFileNameWithoutExtension(MyFilePath).ToLower())
                                {
                                    string MyFile = Path.GetFileName(MyFilePath);
                                    readText = fileline.Replace(readText, @"FILE """ + MyFile + @"""");
                                    break;
                                }
                            }
                        if (!MultiMusicFileType) //�����û��ƥ��ĵ���ȴ��ֻ��һ���������ͣ���ô�ô��滻�˰գ��ף�
                            readText = fileline.Replace(readText, @"FILE ""$1" + MyExt + @"""");
                    }
                }
            }
            FileContentTextBox.Text = readText;

            // �Զ�ģʽ            
            if (AutoOutputCheckBox.Checked) output();
        }

        private string getcodetype(string path)
        {
            string strCodeType = "CODETYPE_UTF8NOBOM";

            Byte[] MyByte = File.ReadAllBytes(path);
            int high, low, chr, i;
            int JP1 = 0, JP2 = 0;
            bool FakeJP = false;

            i = 0;
            bool isUTF8 = true;
            while (i < MyByte.Length)
            {
                if ((0x80 & MyByte[i]) == 0) // ASCII
                {
                    i++;
                    continue;
                }
                else if ((0xE0 & MyByte[i]) == 0xC0) // 110xxxxx
                {
                    if (i + 1 > MyByte.Length)
                    {
                        isUTF8 = false;
                        break;
                    }
                    if ((0xC0 & MyByte[i + 1]) == 0x80) // 10xxxxxx
                    {
                        i += 2;
                        continue;
                    }
                    else
                    {
                        isUTF8 = false;
                        break;
                    }
                }
                else if ((0xF0 & MyByte[i]) == 0xE0) // 1110xxxx
                {
                    if (i + 1 > MyByte.Length)
                    {
                        isUTF8 = false;
                        break;
                    }
                    if (i + 2 > MyByte.Length)
                    {
                        isUTF8 = false;
                        break;
                    }
                    if (((0xC0 & MyByte[i + 1]) == 0x80) && ((0xC0 & MyByte[i + 2]) == 0x80)) // 10xxxxxx 10xxxxxx
                    {
                        i += 3;
                        continue;
                    }
                    else
                    {
                        isUTF8 = false;
                        break;
                    }
                }
                else // ����UTF-8�ַ���
                {
                    isUTF8 = false;
                    break;
                }
            }

            if (isUTF8 == false)
                strCodeType = "CODETYPE_SHIFTJIS";

            if (strCodeType == "CODETYPE_SHIFTJIS")
            {
                for (i = 0; i < MyByte.Length; )
                {
                    high = MyByte[i]; //��ȡ��һ��byte
                    i++;
                    if (high <= 0x7F)  //ASCII����
                    {
                        low = high;
                        high = 0;
                    }
                    else if ((high >= 0xA1) && (high <= 0xDF))  //���Ƭ������
                    {
                        low = high;
                        high = 0;
                        JP1++;
                    }
                    else  //˫�ֽ���
                    {
                        low = MyByte[i]; //��ȡ��λ
                        i++;
                        JP2++;
                    }
                    chr = low + high * 256;

                    if (chr < 0x80) // ASCII
                    { }
                    else if (chr < 0xA1) // 0x80 - 0xA0 δ����ռ�
                    {
                        strCodeType = "CODETYPE_DEFAULT"; // δ֪����
                        break;
                    }
                    else if (chr < (0xA1 + 63)) // 0xA1 - 0xDF ��Ǽ�����
                    { }
                    else if (chr < 0x8140) // 0xE0 - 0x813F δ����ռ�
                    {
                        strCodeType = "CODETYPE_DEFAULT";  // δ֪����
                        break;
                    }
                    else // 0x8140 - 0xFFFF
                    {
                        char a = JISMapBuffer[chr - 0x8140 + 63];
                        if (a == '\uFFFD')
                        {
                            strCodeType = "CODETYPE_GBK";
                            break;
                        }
                    }
                }
            }
            

            if ((strCodeType == "CODETYPE_SHIFTJIS")&&((float)JP1/JP2 >= 1.8))
            {
                FakeJP = true;
                strCodeType = "CODETYPE_GBK";
            }

            if (strCodeType == "CODETYPE_GBK")
            {
                for (i = 0; i < MyByte.Length; )
                {
                    high = MyByte[i]; //��ȡ��һ��byte
                    i++;
                    if (high > 0x7F) //��һ��byte�Ǹ�λ
                    {
                        low = MyByte[i]; //��ȡ��λ
                        i++;
                    }
                    else
                    {
                        low = high;
                        high = 0;
                    }

                    chr = low + high * 256;
                    if (chr < 0x80) // ASCII��
                    { }
                    else if (chr < 0x8140) // 0x80 - 0x813F δ����ռ�
                    {
                        strCodeType = "CODETYPE_DEFAULT";   // δ֪����
                        break;
                    }
                    else
                    {
                        char a = GBMapBuffer[chr - 0x8140];
                        if (a == '\uFFFD')
                        {
                            strCodeType = "CODETYPE_BIG5";
                            break;
                        }
                    }
                }
            }

            if (strCodeType == "CODETYPE_BIG5")
            {
                for (i = 0; i < MyByte.Length; )
                {
                    high = MyByte[i]; //��ȡ��һ��byte
                    i++;
                    if (high > 0x7F) //��һ��byte�Ǹ�λ
                    {
                        low = MyByte[i]; //��ȡ��λ
                        i++;
                    }
                    else
                    {
                        low = high;
                        high = 0;
                    }
                    chr = low + high * 256;
                    if (chr < 0x80) // ASCII��
                    { }
                    else if (chr < 0x8140) // 0x80 - 0x813F δ����ռ�
                    {
                        strCodeType = "CODETYPE_DEFAULT";   // δ֪����
                        break;
                    }
                    else
                    {
                        char a = Big5MapBuffer[chr - 0x8140];
                        if (a == '\uFFFD')
                        {
                            strCodeType = "CODETYPE_DEFAULT";
                            break;
                        }
                    }
                }
            }
            if ((strCodeType == "CODETYPE_DEFAULT") && FakeJP)
                strCodeType = "CODETYPE_SHIFTJIS";

            return strCodeType;
        }

        private void output() 
        {            
            StreamWriter sw = new StreamWriter(FilePathTextBox.Text, false, Encoding.UTF8);
            sw.Write(FileContentTextBox.Text);
            sw.Close();

            if (Path.GetExtension(original_path).ToLower() == ".cue")
            {
                // �Ƿ��Զ����͵�������
                if (AutoSend2PlayerCheckBox.Checked)
                {
                    System.Diagnostics.Process.Start(PlayerPathTextBox.Text, paremeterTextBox.Text.Replace("%1", FilePathTextBox.Text));
                }
                //�Ƿ��Զ���������Cue�ļ�
                if (AutoDealOtherCueCheckBox.Checked)
                {
                    if (musicfiles.Count == 0)  //���û���ѵ����������ļ��������κ�����
                    { }
                    else if (musicfiles.Count == 1)  //���ֻ�ѵ�һ������ô������������cue
                    {
                        DealOtherCue(false);
                    }
                    else
                    {
                        DealOtherCue(true);  //����ж�����������ļ�����ô���������cue������ͬһ�������ļ���cue
                    }                   

                }
            }

            musicfiles.Clear();
            FilePathTextBox.Text = "";
            FileContentTextBox.Text = "";
            OutputButton.Enabled = false;
            loaded = false;

            // �Ƿ��Զ��˳�
            if ((AutoExitCheckBox.Checked) && (AutoExitCheckBox.Enabled))
            {
                Application.Exit();
            }
        }

        private void DealOtherCue(bool MultiMusicFile)
        {
            List<string> CueFile = new List<string>();
            CueFile.AddRange(Directory.GetFiles(Path.GetDirectoryName(original_path), "*.cue"));
            for (int i = 0; i < CueFile.Count; i++)
            {
                if (CueFile[i] != FilePathTextBox.Text)
                {
                    if (!MultiMusicFile)
                    {
                        DealOtherCueChild(CueFile[i]);
                    }
                    else
                    {
                        if (FileInFileLine == OpenFile(CueFile[i]))
                        {
                            DealOtherCueChild(CueFile[i]);
                        }
                    }
                }
            }
        }

        private void DealOtherCueChild(string path)
        {
            if (radioButton1.Checked)
            {
                FileAttributes fa = File.GetAttributes(path);
                File.SetAttributes(path, FileAttributes.Hidden | fa);
            }
            else if (radioButton2.Checked)
            {
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);                
            }
            else if (radioButton3.Checked)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(original_path) + @"\" + SubDirTextBox.Text + @"\");
                File.Move(path, Path.GetDirectoryName(original_path) + @"\" + SubDirTextBox.Text + @"\" + Path.GetFileName(path));
            }

        }

        private void button1_Click(object sender, EventArgs e)   //ͨ�����򿪡��Ի�����ļ�
        {            
            
            if (OpenFileDialog.ShowDialog() == DialogResult.OK)
            {                
                original_path = OpenFileDialog.FileName;
                main_func();
            }       
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)  //ͨ����ק���ļ�
        {            
            original_path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            main_func(); 
        }


        private void Form1_DragEnter_1(object sender, DragEventArgs e)  //ͨ����ק���ļ�����
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }

        //private void ReadConfig(object output, string IniDir, string IniValue)
        //{
        //    string MyValue = null;
        //    try
        //    {
        //        MyValue = config.IniReadValue(IniDir, IniValue);
        //    }
        //    catch
        //    {
        //    }
        //    if (!String.IsNullOrEmpty(MyValue))
        //    {
        //        if (Regex.IsMatch(MyValue, "^[0-9]+$"))
        //        {
        //            output = Convert.ToInt16(MyValue);
        //        }
        //        else if (Regex.IsMatch(MyValue, "true|flae", RegexOptions.IgnoreCase))
        //        {
        //            output = Convert.ToBoolean(MyValue);
        //        }
        //        else
        //        {
        //            output = MyValue;
        //        }
        //    }
        //}

        private void Form1_Load(object sender, EventArgs e)   //��ȡ���ã���ʼ��
        {
            string[] com = Environment.GetCommandLineArgs();
            comboBox1.Text = "932";
        
            if (File.Exists(Application.StartupPath + @"\config.ini"))
            {
                try
                {
                    //int MyLeft = -99, Mytop = -99;
                    //ReadConfig(MyLeft, "Location", "x");
                    //ReadConfig(Mytop, "Location", "y");
                    int MyLeft = Convert.ToInt16(config.IniReadValue("Location", "x"));
                    int Mytop = Convert.ToInt16(config.IniReadValue("Location", "y"));
                    if ((MyLeft != -99) && (Mytop != -99))
                        this.Location = new Point(MyLeft, Mytop);
                    RenameTextBox.Text = config.IniReadValue("Settings", "regex_name");
                    comboBox1.Text = config.IniReadValue("Settings", "code");                    
                    CoverOldFileCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "overwrite"));
                    AutoOutputCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "auto"));
                    ttafixCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "ttatowave"));
                    fileLineCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "fixeacname"));
                    AutoExitCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "autoexit"));
                    AutoDealOtherCueCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "autodealothercue"));
                    AutoSend2PlayerCheckBox.Checked = Convert.ToBoolean(config.IniReadValue("Settings", "sendtoplayer"));
                    PlayerPathTextBox.Text = config.IniReadValue("Settings", "playerpath");
                    paremeterTextBox.Text = config.IniReadValue("Settings", "playercommand");
                    SubDirTextBox.Text = config.IniReadValue("Settings", "subdir");
                    foreach (Control a in this.groupBox1.Controls)
                    {
                        if (a.Name == "radioButton" + config.IniReadValue("Settings", "cuedealmethod"))
                        {
                            ((RadioButton)a).Checked = true;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                }
            }            
            if (com.Length != 1)
            {
                original_path = com[1];
                main_func();
            }            
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)   //��������
        {
            config.IniWriteValue("Location", "x", this.Location.X.ToString());
            config.IniWriteValue("Location", "y", this.Location.Y.ToString());
            config.IniWriteValue("Settings", "regex_name", RenameTextBox.Text);
            config.IniWriteValue("Settings", "code", comboBox1.Text);
            config.IniWriteValue("Settings", "overwrite",CoverOldFileCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "auto", AutoOutputCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "ttatowave", ttafixCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "fixeacname", fileLineCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "autoexit", AutoExitCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "sendtoplayer", AutoSend2PlayerCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "autodealothercue", AutoDealOtherCueCheckBox.Checked.ToString());
            config.IniWriteValue("Settings", "playerpath", PlayerPathTextBox.Text);
            config.IniWriteValue("Settings", "playercommand", paremeterTextBox.Text);
            config.IniWriteValue("Settings", "subdir", SubDirTextBox.Text);
            foreach (Control a in this.groupBox1.Controls)
            { 
                if (a.Name.Contains("radioButton") && ((RadioButton)a).Checked)
                {
                    config.IniWriteValue("Settings", "cuedealmethod", a.Name.Replace("radioButton",""));
                    break;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)   // �ֶ����
        {
            output();    
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)  // �Ƿ��滻Դ�ļ�
        {
            
            if (CoverOldFileCheckBox.Checked) 
            {
                RenameTextBox.Enabled = false;
                FilePathTextBox.Text = original_path;
              //  RenameTextBox.Text = "%filename%";
            }
            else 
            {
                RenameTextBox.Enabled = true;
                ChangeOutputFilePath();
              //  RenameTextBox.Text = temp;
            }            
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)  //�Զ�ģʽʱ���á��������ť
        {
            if (AutoOutputCheckBox.Checked)
            {
                OutputButton.Enabled = false;
                AutoExitCheckBox.Enabled = true;
            }
            else
            {
                if (loaded) OutputButton.Enabled = true;
                AutoExitCheckBox.Enabled = false;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (!CoverOldFileCheckBox.Checked)
                ChangeOutputFilePath();
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoSend2PlayerCheckBox.Checked)
            {
                PlayerPathTextBox.Enabled = true;
                paremeterTextBox.Enabled = true;
                button3.Enabled = true;
            }
            else
            {
                PlayerPathTextBox.Enabled = false;
                paremeterTextBox.Enabled = false;
                button3.Enabled = false;
            }
        }

        private void AutoDealOtherCueCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoDealOtherCueCheckBox.Checked)
            {
                radioButton1.Enabled = true;
                radioButton2.Enabled = true;
                radioButton3.Enabled = true;
                SubDirTextBox.Enabled = true;
            }
            else
            {
                radioButton1.Enabled = false;
                radioButton2.Enabled = false;
                radioButton3.Enabled = false;
                SubDirTextBox.Enabled = false;
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (OpenPlayerDialog.ShowDialog() == DialogResult.OK)
            {
                PlayerPathTextBox.Text = OpenPlayerDialog.FileName;                
            }           
        }

        private void button4_Click(object sender, EventArgs e) // ��ӵ�ע���
        {
            if (IsUserAdministrator())
            {
                RegistryKey Root = Registry.ClassesRoot;
                RegistryKey software = null;
                RegistryKey CurrentUser = Registry.CurrentUser;

                if ((System.Environment.OSVersion.Version.Major == 5) || ((System.Environment.OSVersion.Version.Major == 6) && (System.Environment.OSVersion.Version.Minor == 0)))
                {
                    software = CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.cue\OpenWithProgids");
                    if (software != null)
                    {
                        foreach (string b in software.GetValueNames())
                        {
                            AddToReg(b);
                        }
                    }
                    string a = "";
                    software = Root.OpenSubKey(".cue");
                    if (software != null)
                        a = software.GetValue("").ToString();
                    if (a != "")
                        AddToReg(a);
                    else
                    {
                        AddToReg(".cue");
                    }
                }

                if ((System.Environment.OSVersion.Version.Major == 6) && (System.Environment.OSVersion.Version.Minor >= 1))
                {
                    string a = "";
                    software = CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.cue\UserChoice");
                    if (software != null)
                    {
                        if (software.GetValue("Progid") != null)
                            a = software.GetValue("Progid").ToString();
                    }
                    if (a != "")
                    {
                        AddToReg(a);
                    }
                    else
                    {
                        software = Root.OpenSubKey(".cue");
                        if (software != null)
                            a = software.GetValue("").ToString();
                        if (a != "")
                            AddToReg(a);
                        else
                        {
                            AddToReg(".cue");
                        }
                    }
                }
                MessageBox.Show("�ѳɹ�ע�ᵽ�Ҽ��˵�!");
            }
            else
            {
                MessageBox.Show("�����������ù���Ա�������!");
            }
        }

        void AddToReg(string a)
        {
            RegistryKey Root = Registry.ClassesRoot;
            RegistryKey cue = Root.CreateSubKey(a);
            RegistryKey shell = cue.CreateSubKey("shell");
            if (shell.OpenSubKey("�� FixCue �޸�") != null)
                shell.DeleteSubKeyTree("�� FixCue �޸�");
            if (shell.OpenSubKey("Fix") != null)
                shell.DeleteSubKeyTree("Fix");
            RegistryKey mystring = shell.CreateSubKey("Fix");
            mystring.SetValue("", "�� FixCue �޸�");
            RegistryKey command = mystring.CreateSubKey("command");
            command.SetValue("", @"""" + Application.ExecutablePath + @""" ""%1""");
        }

        private void button5_Click(object sender, EventArgs e) //��ע����Ƴ�
        {
            if (IsUserAdministrator())
            {
                RegistryKey CurrentUser = Registry.CurrentUser;
                RegistryKey Root = Registry.ClassesRoot;
                RegistryKey software = null;

                if ((System.Environment.OSVersion.Version.Major == 5) || ((System.Environment.OSVersion.Version.Major == 6) && (System.Environment.OSVersion.Version.Minor == 0)))
                {
                    software = CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.cue\OpenWithProgids");
                    if (software != null)
                    {
                        foreach (string b in software.GetValueNames())
                        {
                            RemoveFromReg(b);
                        }
                    }
                    string a = "";
                    software = Root.OpenSubKey(".cue");
                    if (software != null)
                        a = software.GetValue("").ToString();
                    if (a != "")
                        RemoveFromReg(a);
                    else
                    {
                        RemoveFromReg(".cue");
                    }
                }

                if ((System.Environment.OSVersion.Version.Major == 6) && (System.Environment.OSVersion.Version.Minor >= 1))
                {

                    string a = "";
                    software = CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.cue\UserChoice");
                    if (software != null)
                    {
                        if (software.GetValue("Progid") != null)
                            a = software.GetValue("Progid").ToString();
                    }
                    if (a != "")
                    {
                        RemoveFromReg(a);
                    }
                    else
                    {
                        software = Root.OpenSubKey(".cue");
                        if (software != null)
                            a = software.GetValue("").ToString();
                        if (a != "")
                            RemoveFromReg(a);
                        else
                        {
                            RemoveFromReg(".cue");
                        }
                    }
                }
                MessageBox.Show("�ѳɹ����Ҽ��˵�ж��!");
            }
            else
            {
                MessageBox.Show("�����������ù���Ա�������!");
            }
        }

        void RemoveFromReg(string a)
        {
            RegistryKey Root = Registry.ClassesRoot;
            RegistryKey cue_shell = Root.OpenSubKey(a + @"\shell", true);
            if (cue_shell != null)
            {
                if (cue_shell.OpenSubKey("�� FixCue �޸�") != null)
                    cue_shell.DeleteSubKeyTree("�� FixCue �޸�");
                if (cue_shell.OpenSubKey("Fix") != null)
                    cue_shell.DeleteSubKeyTree("Fix");
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.Width >= 817)
            { pictureBox1.Left = this.Width - 56; }
            else
                pictureBox1.Left = 761;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (loaded)
            {
                forcecode = true;
                main_func();
            }
        }
    }

    public class INIClass
    {
        public string inipath;
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
        /// 
        /// ���췽��
        /// 
        /// �ļ�·��
        public INIClass(string INIPath)
        {
            inipath = INIPath;
        }
        /// 
        /// д��INI�ļ�
        /// 
        /// ��Ŀ����(�� [TypeName] )
        /// ��
        /// ֵ
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
        /// 
        /// ����INI�ļ�
        /// 
        /// ��Ŀ����(�� [TypeName] )
        /// ��
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(500);
            int i = GetPrivateProfileString(Section, Key, "", temp, 500, this.inipath);
            return temp.ToString();
        }
        /// 
        /// ��֤�ļ��Ƿ����
        /// 
        /// ����ֵ
        public bool ExistINIFile()
        {
            return System.IO.File.Exists(inipath);
        }
    }   

}