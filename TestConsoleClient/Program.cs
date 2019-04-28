using IComm_Library;
using OPC_UA_Library;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace TestConsoleClient
{
  class Program
  {
    private static OPCTag T0;
    private static OPCTag T1;
    private static OPCTag T2;
    private static OPCTag T;
    public static ObservableCollection<OPCTag> Tag_List;

    private static OPCClient MyClient;
    private static List<string> tagsToRead;

    static ConcurrentQueue<Tuple<bool, long>> concurrentQueue;


    static void Main(string[] args)
    {
      try
      {
        Tag_List = new ObservableCollection<OPCTag>();

        CustomLogger logger = new CustomLogger();

        var opcConnectionString = @"opc.tcp://127.0.0.1:49320";
        //var opcConnectionString = @"opc.tcp://10.0.2.170:49320";

        //var endpoint1 = OPCCoreUtils.SelectEndpointBySecurity(opcConnectionString, SecurityPolicy.Basic128, MessageSecurityMode.Sign, 10000);


        MyClient = new OPCClient(opcConnectionString, "", "", logger, "Mytest2", SecurityPolicy.Basic128, MessageSecurity.Sign);

         T = (OPCTag)MyClient.AddTag("RAMP1", "ns=2;s=TEST_OPC.RAMP.RAMP1", typeof(float));
        T0 = (OPCTag)MyClient.AddTag("WRITE1", "ns=2;s=TEST_OPC.WRITE.WRITE1", typeof(float));
        T1 = (OPCTag)MyClient.AddTag("WRITE2", "ns=2;s=TEST_OPC.WRITE.WRITE2", typeof(float));
        T2 = (OPCTag)MyClient.AddTag("WRITE3", "ns=2;s=TEST_OPC.WRITE.WRITE3", typeof(float));
        float val = 5;
        var writeResult = T0.WriteItem(val);
        T0.ReadItem();

        var T3 = (OPCTag)MyClient.AddTag("CAst", "ns=2;s=TEST_OPC.WRITE.WRITESLOW1", typeof(double));
        T3.ReadItem();
        Console.WriteLine(T3.Value);

        //T0 = (OPCTag)MyClient.AddTag("Write01", "ns=4;s=MAIN.iMS0", typeof(short));
        //T0.WriteItem((short)5);

        //T0.ReadItem();

        //val = 100f;
        //T0.WriteItem(val);
        //T0.ReadItem();

        //T0.SubscribeItem(null,null, Newvalue);
        List<string> TagNameList = new List<string>();
        List<object> TagObjectList = new List<object>();
        TagNameList.Add(T0.Name);
        TagNameList.Add(T1.Name);
        TagNameList.Add(T2.Name);
        TagObjectList.Add(101f);
        TagObjectList.Add(102f);
        TagObjectList.Add(103f);
        bool Readok = MyClient.ReadTags(TagNameList);

        bool Writeok = MyClient.WriteTags(TagNameList, TagObjectList);

        //Tag_List.Add(T);
        //Tag_List.Add(T);

        //this.DataContext = Tag_List;

        ////T.WriteItem(10);

        // Read multiple variables at once
        var baseName1 = "ns=2;s=TEST_OPC.RAMP.RAMP";
        var baseName2 = "ns=2;s=TEST_OPC.RAND.RAND";

        tagsToRead = new List<string>();
        for (int i=1; i<=150; i++)
        {
          var tagName1 = $"{baseName1}{i}";
          MyClient.AddTag(tagName1, tagName1, typeof(float));
          tagsToRead.Add(tagName1);

          var tagName2 = $"{baseName2}{i}";
          MyClient.AddTag(tagName2, tagName2, typeof(int));
          tagsToRead.Add(tagName2);
        }

        concurrentQueue = new ConcurrentQueue<Tuple<bool, long>>();

        Timer _timer = new Timer(50);
        _timer.Elapsed += _timer_Elapsed;
        _timer.Start();

        Timer _health = new Timer(1000);
        _health.Elapsed += _health_Elapsed;
        _health.Start();

        //MyClient.SubScribeTag(T, 0, 0, null);
        MyClient.SubscribeTag(T0, 0, 0, Newvalue);
        //var name = Console.ReadLine();

        Console.ReadKey();
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }

    }

    private static void _health_Elapsed(object sender, ElapsedEventArgs e)
    {
      int deq = concurrentQueue.Count;
      Console.WriteLine($"QC: {deq}\n");

      int good = 0, bad = 0;
      double avgGood = 0, avgBad = 0;

      for (int i = 0; i < deq; i++)
      {
        Tuple<bool, long> res;
        concurrentQueue.TryDequeue(out res);

        if (res.Item1)
        {
          // good
          good += 1;
          avgGood += res.Item2;
        }
        else
        {
          // bad
          bad += 1;
          avgBad += res.Item2;
        }
      }

      avgGood /= (good == 0)? 1 : good;
      avgBad /= (bad == 0)? 1 : bad;

      Console.WriteLine($"Good {good}, {avgGood} ms\n");
      Console.WriteLine($"Bad {bad}, {avgBad} ms\n");
    }

    private static void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      Stopwatch sw = new Stopwatch();
      sw.Start();
      bool readRandRamp = MyClient.ReadTags(tagsToRead);
      sw.Stop();

      concurrentQueue.Enqueue(new Tuple<bool, long>(readRandRamp, sw.ElapsedMilliseconds));
    }

    private static void Newvalue(ITag changedTag)
    {
      //Console.Write($"value: {changedTag.Value}");
      //Application.Current.Dispatcher.Invoke(() =>
      //{
      //    TagValue.Text = changedTag.Value.ToString();
      //    TagTimeStamp.Text = changedTag.LastUpdate.ToString();
      //});

    }

  }
}
