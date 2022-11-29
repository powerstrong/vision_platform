﻿using Newtonsoft.Json.Linq;
using PLAIF_VisionPlatform.Model;
using Rosbridge.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using static PLAIF_VisionPlatform.Model.RosbridgeModel;

namespace PLAIF_VisionPlatform.ViewModel
{
    public sealed class RosbridgeMgr
    {
        private MessageDispatcher _md;
        private bool _isConnected = false;
        private MainViewModel _mainViewModel;
        private RosbridgeModel _rosbrdgModel;

        List<Subscriber> _subscribers;

        private RosbridgeMgr() 
        {
            _rosbrdgModel = new RosbridgeModel();
            _subscribers = new List<Subscriber>();
        }
        private static readonly Lazy<RosbridgeMgr> _instance = new Lazy<RosbridgeMgr>(() => new RosbridgeMgr());
        public static RosbridgeMgr Instance => _instance.Value;

        public void SetMainModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public async void Connect(string uri)
        {
            if (_isConnected)
            {
                foreach (var s in _subscribers)
                {
                    s.UnsubscribeAsync().Wait();
                }
                _isConnected = false;
                _subscribers.Clear();

                await _md.StopAsync();
                _md = null;
            }
            else
            {
                try
                {
                    _md = new MessageDispatcher(new Socket(new Uri(uri)), new MessageSerializerV2_0());
                    _md.StartAsync().Wait();
                    
                    foreach (var tuple in _rosbrdgModel.GetSubscribeTopics())
                    {
                        SubscribeMsg(tuple.Item1, tuple.Item2);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,
                         "Error!! Could not connect to the rosbridge server", MessageBoxButton.OK, MessageBoxImage.Error);
                    _md = null;
                    return;
                }

                _isConnected = true;
            }
            //ToggleConnected();
        }

        public bool IsConnected() { return _isConnected; }
        public async void Capture()
        {
            ServiceCallMsg("/zivid_camera/capture", "[]");
        }

        private async void PublishMsg(string topic, string msg_type, string msg)
        {
            var pb = new Rosbridge.Client.Publisher(topic, msg_type, _md);
            await pb.PublishAsync(JObject.Parse(msg));
        }
        
        private void _subscriber_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            string msg = e.Message["msg"]!.ToString();

            switch (e.Message["topic"]!.ToString())
            {
                case RosbridgeModel.RosTopics.chatter:
                    Debug.Print("[chatter] : " + msg); // 메시지 크기가 큰 경우 주의할 것
                    break;
                case RosbridgeModel.RosTopics.zvd_point_xyz:
                    //_mainViewModel.Create2DBitMapImage(e.Message["msg"]["data"].ToString());
                    break;
                case RosTopics.zvd_color_image:
                    _mainViewModel.Create2DBitMapImage(e.Message["msg"]["data"].ToString());
                    break;
            }

        }

        private async void SubscribeMsg(string topic, string msg_type)
        {
            var s = new Subscriber(topic, msg_type, _md);
            s.MessageReceived += _subscriber_MessageReceived;
            await s.SubscribeAsync();
            _subscribers.Add(s);
        }

        private async void ServiceCallMsg(string topic, string msg)
        {
            var sc = new ServiceClient(topic, _md);
            JArray argsList = JArray.Parse(msg);
            var result = await sc.Call(argsList.ToObject<List<dynamic>>());

            // UI 쓰레드 접근 시 사용..하나 여기선 밖에서 사용해야 할듯
            //Dispatcher.Invoke(() =>
            //{
            //    try
            //    {
            //        s = result.ToString();
            //    }
            //    catch { }
            //});
        }
    }
}
