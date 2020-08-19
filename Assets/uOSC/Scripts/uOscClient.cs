using UnityEngine;
using System.IO;
using System.Collections.Generic;


namespace uOSC
{

    public class uOscClient : MonoBehaviour
    {

        private const int BufferSize = 8192;
        private const int MaxQueueSize = 100;
 //       [SerializeField]
 //       //string address = "224.0.0.1"; // for multicast
 //       string address = "192.168.1.21"; // for unicast
 //       [SerializeField]
 //       string address1 = "192.168.1.23";
 //       [SerializeField]
 //       string address2 = "192.168.1.26";


        [SerializeField]
        int port = 4444;

#if NETFX_CORE
    Udp udp_ = new Uwp.Udp();
    Thread thread_ = new Uwp.Thread();
#else

     
    // for Multicast
    //Udp udp_ = new DotNetMulti.Udp();
    //Thread thread_ = new DotNetMulti.Thread();

    // for Unicast
    Udp udp_ = new DotNet.Udp();
    Thread thread_ = new DotNet.Thread();
   

#endif
Queue<object> messages_ = new Queue<object>();
    object lockObject_ = new object();

    public void Address()
    {
        udp_.StartClient(IPportGetter.IP1, port);
           
            thread_.Start(UpdateSend);
    }

        public void Address1()
        {
            udp_.StartClient(IPportGetter.IP2, port);

            thread_.Start(UpdateSend);
        }
        public void Address2()
        {
            udp_.StartClient(IPportGetter.IP3, port);

            thread_.Start(UpdateSend);
        }
    void OnDisable()
    {
        thread_.Stop();
        udp_.Stop();
    }

    void UpdateSend()
    {
        while (messages_.Count > 0)
        {
            object message;
            lock (lockObject_)
            {
                message = messages_.Dequeue();
            }

            using (var stream = new MemoryStream(BufferSize))
            {
                if (message is Message)
                {
                    ((Message)message).Write(stream);
                }
                else if (message is Bundle)
                {
                    ((Bundle)message).Write(stream);
                }
                else
                {
                    return;
                }
                udp_.Send(Util.GetBuffer(stream), (int)stream.Position);
            }
        }
    }

    void Add(object data)
    {
        lock (lockObject_)
        {
            messages_.Enqueue(data);

            while (messages_.Count > MaxQueueSize)
            {
                messages_.Dequeue();
            }
        }
    }

    public void Send(string address, params object[] values)
    {
        Send(new Message() 
        {
            address = address,
            values = values
        });
    }

    public void Send(Message message)
    {
        Add(message);
    }

    public void Send(Bundle bundle)
    {
        Add(bundle);
    }
}

}