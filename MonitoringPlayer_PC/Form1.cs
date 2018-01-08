using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;

namespace MonitoringPlayer_PC
{
    public partial class Form1 : Form
    {
        VLCPlayer player;
        static int cnt = 0; // PLAYING 아닐 경우 카운트 증가
        static string sendSMS = "N"; //문자 발송 체크

        static int playerRestart = 3; //장애시 5초마다 플레이어 재시작
        static int sendFirstSMS = 10; //최초 문자발송 30초
        static int sendNextSMS = 20; //최초 발송 후 5분마다 재발송

        public Form1()
        {
            InitializeComponent();

            // 스트리밍 RTMP
            // TEST : @"rtmp://110.93.135.133/live/myStream"
            // 불교방송(PC) : @"rtmp://bbstv.clouducs.com/bbstv-live/livestream"
            // 불교방송(MOBILE) : @"rtmp://bbstv.clouducs.com/bbstv-mlive/livestream"

            axVLCPlugin21.playlist.add(@"rtmp://110.93.135.133/live/myStream");
            axVLCPlugin21.playlist.play();

            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Enabled = true;

            Timer boxTimer = new Timer();
            boxTimer.Interval = 5000;
            boxTimer.Tick += new EventHandler(boxTimerTick);
            boxTimer.Enabled = true;
        }

        private void boxTimerTick(object sender, EventArgs e)
        {
            player = new VLCPlayer();
            string state = player.playerState(axVLCPlugin21.input.state); // input state 값을 문자열 상태로 변환
            textBox2.Text = state;
        }

        // 모니터링 주요 로직
        private void Timer_Tick(object sender, EventArgs e)
        {
            player = new VLCPlayer();
            string state = player.playerState(axVLCPlugin21.input.state); // input state 값을 문자열 상태로 변환
            
            System.Console.WriteLine("plugin fps : "+ axVLCPlugin21.input.fps); // 1만찍힘
            
                
            LogFile logFile = new LogFile();

            if (state != "PLAYING")
            {
                cnt++; // 에러 카운트 증가
                logFile.log("error state : " + state + ", error count : " + cnt);
                if (cnt % playerRestart == 0)
                {
                    axVLCPlugin21.playlist.stop();
                    axVLCPlugin21.playlist.play();
                    logFile.log("Player restart!");
                }

                // 최초 문자는 문제 발생 후 30초, 1회 발송 후 3분 간격으로 발송
                if (sendSMS == "N" && cnt % sendFirstSMS == 0)
                {
                    System.Diagnostics.Process.Start("http://"); //테스트 URL 열기
                                                                              //System.Diagnostics.Process.Start("http://www.kidis.co.kr/sms_gmedia_send.php"); //문자 발송

                    logFile.log("Send error msg! / " + sendSMS);
                    sendSMS = "Y";
                }
                else if (sendSMS == "Y" && cnt % sendNextSMS == 0)
                {
                    System.Diagnostics.Process.Start("http://"); //테스트 URL 열기
                    //System.Diagnostics.Process.Start("http://www.kidis.co.kr/sms_gmedia_send.php"); //문자 발송
                    logFile.log("Send error msg! / " + sendSMS);
                }
            }
            else
            {
                cnt = 0;
                sendSMS = "N";
                logFile.log("player state : " + state + ", error count : " + cnt);
            }
        }
    }
}

// 로그파일 생성
class LogFile
{
    public string getDateTime()
    {
        DateTime nowDate = DateTime.Now;
        return nowDate.ToString("yyyy-MM-dd HH:mm:ss") + " : " + nowDate.Millisecond.ToString("000");
    }

    //로그내용
    public void log(string str)
    {
        string dirPath = @"C:\Logs\" + DateTime.Today.ToString("yyyy") + @"\PC";
        string filePath = dirPath + @"\Log_" + DateTime.Today.ToString("yyyMMdd") + ".text";
        string temp;

        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
            FileInfo fileInfo = new FileInfo(filePath);
            if (dirInfo.Exists != true)
                Directory.CreateDirectory(dirPath);

            if (fileInfo.Exists != true)
            {
                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    temp = string.Format("[{0}] : {1}", getDateTime(), str);
                    sw.WriteLine(temp);
                    sw.Close();
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    temp = string.Format("[{0}] : {1}", getDateTime(), str);
                    sw.WriteLine(temp);
                    sw.Close();
                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString());
        }
    }
}

class VLCPlayer
{
    public string playerState(int stateNum)
    {
        string inputState = null;
        switch (stateNum)
        {
            case 0:
                inputState = "IDLE";
                break;
            case 1:
                inputState = "OPENING";
                break;
            case 2:
                inputState = "BUFFERING";
                break;
            case 3:
                inputState = "PLAYING";
                break;
            case 4:
                inputState = "PAUSED";
                break;
            case 5:
                inputState = "STOPPING";
                break;
            case 6:
                inputState = "ENDED";
                break;
            case 7:
                inputState = "ERROR";
                break;
        }
        return inputState;
    }
}