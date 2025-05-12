using System;
using TuioNet.Common;
using TuioUnity.Utils;
using UnityEngine;
namespace TuioUnity.Common
{
    /// <summary>
    /// A Tuio-Session is responsible for the communication between the tuio sender and the unity application. It establishes
    /// a connection via UDP or Websocket depending on the given network settings and registers the appropriate callbacks on
    /// the events based on the used tuio version.
    /// </summary>
    public class TuioSessionBehaviour : MonoBehaviour
    {
        [field: SerializeField] public TuioVersion TuioVersion { get; set; } = TuioVersion.Tuio11;
        [field: SerializeField] public TuioConnectionType ConnectionType { get; set; } = TuioConnectionType.UDP;
        [SerializeField] private string _ipAddress = "10.0.0.20";
        [field: SerializeField] public int UdpPort { get; set; }= 3333;

        private TuioSession _session;
        private bool _isInitialized;
        private UnityLogger _logger;

        public ITuioDispatcher TuioDispatcher
        {
            get
            {
                if (_session == null && !_isInitialized)
                {
                    Debug.LogWarning("[TuioSessionBehaviour] Accessing Dispatcher before Awake/Initialize. Initializing now.");
                    Initialize();
                }
                if(_session == null)
                {
                    Debug.LogError("[TuioSessionBehaviour] TUIO Session is null when accessing Dispatcher. Initialization might have failed.");
                    return null;
                }
                return _session.TuioDispatcher;
            }
        }

        public string IpAddress => _ipAddress;

        private void Awake()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
        
        private void Initialize()
        {
            if (_isInitialized) 
            {
                Debug.LogWarning("[TuioSessionBehaviour] Initialize called but already initialized. Skipping.");
                return;
            }
            
            Debug.Log($"[TuioSessionBehaviour] Initializing TUIO {TuioVersion} {ConnectionType} connection to {_ipAddress}:{UdpPort}");

            try
            {
                _logger = new UnityLogger(); 
                var port = UdpPort;
                if (ConnectionType == TuioConnectionType.Websocket)
                {
                     port = TuioVersion switch
                    {
                        TuioVersion.Tuio11 => 3333,
                        TuioVersion.Tuio20 => 3343,
                        _ => throw new ArgumentOutOfRangeException($"{typeof(TuioVersion)} has no value of {TuioVersion}.")
                    };
                    if (port != UdpPort) 
                    {
                        Debug.LogWarning($"[TuioSessionBehaviour] Websocket connection type selected. Using default port {port} instead of configured UdpPort {UdpPort}.");
                    }
                }
                
                _session = new TuioSession(_logger, TuioVersion, ConnectionType, _ipAddress, port, false);
                _isInitialized = true;
                Debug.Log("[TuioSessionBehaviour] Initialization successful.");
            }
            catch (System.Net.Sockets.SocketException e)
            {
                 Debug.LogError($"[TuioSessionBehaviour] SocketException during initialization (Port {UdpPort} likely in use): {e.Message}");
                 _session = null;
                 _isInitialized = false;
            }
            catch (Exception e)
            {
                 Debug.LogError($"[TuioSessionBehaviour] Exception during initialization: {e}");
                 _session = null;
                 _isInitialized = false;
            }
        }

        public void AddMessageListener(MessageListener listener)
        {
            if (_session == null && !_isInitialized) Initialize();
            if (_session != null)
            {
                 _session.AddMessageListener(listener);
            }
            else
            {
                Debug.LogError("[TuioSessionBehaviour] Cannot add listener: TUIO session is not initialized.");
            }
        }

        public void RemoveMessageListener(string messageProfile)
        {
            if (_session != null)
            {
                 _session.RemoveMessageListener(messageProfile);
            }
        }

        public void RemoveMessageListener(MessageListener listener)
        {
             if (_session != null)
            {
                RemoveMessageListener(listener.MessageProfile);
            }
        }

        private void Update()
        {
            if (_isInitialized && _session != null)
            {
                 try 
                 {
                    _session.ProcessMessages();
                 }
                 catch (Exception e)
                 {
                    Debug.LogError($"[TuioSessionBehaviour] Error processing TUIO messages: {e}");
                 }
            }
        }

        private void OnDestroy()
        {
             Debug.Log($"[TuioSessionBehaviour] OnDestroy called. Cleaning up TUIO session for port {UdpPort}.");
            DisposeSession();
        }

        private void OnApplicationQuit()
        {
             Debug.Log("[TuioSessionBehaviour] OnApplicationQuit called. Ensuring TUIO session is disposed.");
            DisposeSession();
        }
        
        private void DisposeSession()
        {
            if (_session != null)
            {
                try
                {
                    _session.Dispose();
                }
                 catch (Exception e)
                {
                    Debug.LogError($"[TuioSessionBehaviour] Exception during session Dispose: {e}");
                }
                finally
                {
                     _session = null;
                }
            }
            _isInitialized = false;
        }
    }
}