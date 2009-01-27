//*******************************************************************************************************
//  ClientBase.cs
//  Copyright © 2008 - TVA, all rights reserved - Gbtc
//
//  Build Environment: C#, Visual Studio 2008
//  Primary Developer: Pinal C. Patel, Operations Data Architecture [PCS]
//      Office: PSO TRAN & REL, CHATTANOOGA - MR BK-C
//       Phone: 423/751-3024
//       Email: pcpatel@tva.gov
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  06/01/2006 - Pinal C. Patel
//       Original version of source code generated
//  09/06/2006 - J. Ritchie Carroll
//       Added bypass optimizations for high-speed client data access
//  11/30/2007 - Pinal C. Patel
//       Modified the "design time" check in EndInit() method to use LicenseManager.UsageMode property
//       instead of DesignMode property as the former is more accurate than the latter
//  02/19/2008 - Pinal C. Patel
//       Added code to detect and avoid redundant calls to Dispose().
//  09/29/2008 - James R Carroll
//       Converted to C#.
//
//*******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Units;
using PCS.Configuration;
using PCS.IO.Compression;
using PCS.Security.Cryptography;

namespace PCS.Communication
{
    /// <summary>
    /// Represents a client involved in server-client communication.
    /// </summary>
    [ToolboxBitmap(typeof(ClientBase))]
    public abstract class ClientBase : Component, IClient, ISupportInitialize, IPersistSettings
    {
        #region [ Members ]

        // Constants

        /// <summary>
        /// Specifies the default value for the <see cref="MaxConnectionAttempts"/> property.
        /// </summary>
        public const int DefaultMaxConnectionAttempts = -1;

        /// <summary>
        /// Specifies the default value for the <see cref="Handshake"/> property.
        /// </summary>
        public const bool DefaultHandshake = false;

        /// <summary>
        /// Specifies the default value for the <see cref="HandshakeTimeout"/> property.
        /// </summary>
        public const int DefaultHandshakeTimeout = 3000;

        /// <summary>
        /// Specifies the default value for the <see cref="HandshakePassphrase"/> property.
        /// </summary>
        public const string DefaultHandshakePassphrase = "6572a33d-826f-4d96-8c28-8be66bbc700e";

        /// <summary>
        /// Specifies the default value for the <see cref="SecureSession"/> property.
        /// </summary>
        public const bool DefaultSecureSession = false;

        /// <summary>
        /// Specifies the default value for the <see cref="ReceiveTimeout"/> property.
        /// </summary>
        public const int DefaultReceiveTimeout = -1;

        /// <summary>
        /// Specifies the default value for the <see cref="ReceiveBufferSize"/> property.
        /// </summary>
        public const int DefaultReceiveBufferSize = 8192;

        /// <summary>
        /// Specifies the default value for the <see cref="Encryption"/> property.
        /// </summary>
        public const CipherStrength DefaultEncryption = CipherStrength.None;

        /// <summary>
        /// Specifies the default value for the <see cref="Compression"/> property.
        /// </summary>
        public const CompressionStrength DefaultCompression = CompressionStrength.NoCompression;

        /// <summary>
        /// Specifies the default value for the <see cref="PersistSettings"/> property.
        /// </summary>
        public const bool DefaultPersistSettings = false;

        /// <summary>
        /// Specifies the default value for the <see cref="SettingsCategory"/> property.
        /// </summary>
        public const string DefaultSettingsCategory = "CommunicationClient";

        // Events

        /// <summary>
        /// Occurs when client is attempting connection to the server.
        /// </summary>
        [Category("Connection"),
        Description("Occurs when client is attempting connection to the server.")]
        public event EventHandler ConnectionAttempt;

        /// <summary>
        /// Occurs when client connection to the server is established.
        /// </summary>
        [Category("Connection"),
        Description("Occurs when client connection to the server is established.")]
        public event EventHandler ConnectionEstablished;

        /// <summary>
        /// Occurs when client connection to the server is terminated.
        /// </summary>
        [Category("Connection"),
        Description("Occurs when client connection to the server is terminated")]
        public event EventHandler ConnectionTerminated;

        /// <summary>
        /// Occurs when an <see cref="Exception"/> is encountered during connection attempt to the server.
        /// </summary>
        [Category("Connection"),
        Description("Occurs when an Exception is encountered during connection attempt to the server.")]
        public event EventHandler<EventArgs<Exception>> ConnectionException;

        /// <summary>
        /// Occurs when server-client handshake, when enabled, cannot be performed within the specified <see cref="HandshakeTimeout"/> time.
        /// </summary>
        [Category("Server"),
        Description("Occurs when server-client handshake, when enabled, cannot be performed within the specified HandshakeTimeout time.")]
        public event EventHandler HandshakeProcessTimeout;

        /// <summary>
        /// Occurs when server-client handshake, when enabled, cannot be performed successfully due to information mismatch.
        /// </summary>
        [Category("Server"),
        Description("Occurs when server-client handshake, when enabled, cannot be performed successfully due to information mismatch.")]
        public event EventHandler HandshakeProcessUnsuccessful;

        /// <summary>
        /// Occurs when the client begins sending data to the server.
        /// </summary>
        [Category("Data"),
        Description("Occurs when the client begins sending data to the server.")]
        public event EventHandler SendDataStart;

        /// <summary>
        /// Occurs when the client has successfully sent data to the server.
        /// </summary>
        [Category("Data"),
        Description("Occurs when the client has successfully sent data to the server.")]
        public event EventHandler SendDataComplete;

        /// <summary>
        /// Occurs when an <see cref="Exception"/> is encountered when sending data to the server.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the <see cref="Exception"/> encountered when sending data to the server.
        /// </remarks>
        [Category("Data"),
        Description("Occurs when an Exception is encountered when sending data to the server.")]
        public event EventHandler<EventArgs<Exception>> SendDataException;

        /// <summary>
        /// Occurs when no data is received from the server for the <see cref="ReceiveTimeout"/> time.
        /// </summary>
        [Category("Data"),
        Description("Occurs when no data is received from the server for the ReceiveTimeout time.")]
        public event EventHandler ReceiveDataTimeout;

        /// <summary>
        /// Occurs when the client receives data from the server.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T1,T2}.Argument1"/> is the buffer containing data received from the server starting at index zero.<br/>
        /// <see cref="EventArgs{T1,T2}.Argument2"/> is the number of bytes received in the buffer from the server.
        /// </remarks>
        [Category("Data"),
        Description("Occurs when the client receives data from the server.")]
        public event EventHandler<EventArgs<byte[], int>> ReceiveDataComplete;

        /// <summary>
        /// Occurs when an <see cref="Exception"/> is encountered when receiving data from the server.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the <see cref="Exception"/> encountered when receiving data from the server.
        /// </remarks>
        [Category("Data"),
        Description("Occurs when an Exception is encountered when receiving data from the server.")]
        public event EventHandler<EventArgs<Exception>> ReceiveDataException;

        // Fields
        private string m_connectionString;
        private int m_maxConnectionAttempts;
        private bool m_handshake;
        private int m_handshakeTimeout;
        private string m_handshakePassphrase;
        private bool m_secureSession;
        private int m_receiveTimeout;
        private int m_receiveBufferSize;
        private CipherStrength m_encryption;
        private CompressionStrength m_compression;
        private bool m_persistSettings;
        private string m_settingsCategory;
        private Encoding m_textEncoding;
        private Action<byte[], int, int> m_receiveDataHandler;
        private ClientState m_currentState;
        private TransportProtocol m_transportProtocol;
        private Guid m_serverID;
        private Guid m_clientID;
        private long m_connectTime;
        private long m_disconnectTime;
        private bool m_disposed;
        private bool m_initialized;
        //private ManualResetEvent m_connectHandle;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the client.
        /// </summary>
        protected ClientBase()
        {
            m_serverID = Guid.Empty;
            m_clientID = Guid.NewGuid();
            m_textEncoding = Encoding.ASCII;
            m_currentState = ClientState.Disconnected;
            m_maxConnectionAttempts = DefaultMaxConnectionAttempts;
            m_handshake = DefaultHandshake;
            m_handshakeTimeout = DefaultHandshakeTimeout;
            m_handshakePassphrase = DefaultHandshakePassphrase;
            m_secureSession = DefaultSecureSession;
            m_receiveTimeout = DefaultReceiveTimeout;
            m_receiveBufferSize = DefaultReceiveBufferSize;
            m_encryption = DefaultEncryption;
            m_compression = DefaultCompression;
            m_persistSettings = DefaultPersistSettings;
            m_settingsCategory = DefaultSettingsCategory;

        }

        /// <summary>
        /// Initializes a new instance of the client.
        /// </summary>
        /// <param name="transportProtocol">One of the <see cref="TransportProtocol"/> values.</param>
        /// <param name="connectionString">The data used by the client for connection to a server.</param>
        protected ClientBase(TransportProtocol transportProtocol, string connectionString)
            : this()
        {
            m_transportProtocol = transportProtocol;
            this.ConnectionString = connectionString;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the data required by the client to connect to the server.
        /// </summary>
        [Category("Settings"),
        Description("The data required by the client to connect to the server.")]
        public virtual string ConnectionString
        {
            get
            {
                return m_connectionString;
            }
            set
            {
                ValidateConnectionString(value);

                m_connectionString = value;
                if (m_currentState == ClientState.Connected)
                {
                    // Reconnect the client when connection data is changed.
                    Disconnect();
                    Connect();
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of times the client will attempt to connect to the server.
        /// </summary>
        /// <remarks>Set <see cref="MaxConnectionAttempts"/> to -1 for infinite connection attempts.</remarks>
        [Category("Settings"),
        DefaultValue(DefaultMaxConnectionAttempts),
        Description("The maximum number of times the client will attempt to connect to the server. Set MaxConnectionAttempts to -1 for infinite connection attempts.")]
        public virtual int MaxConnectionAttempts
        {
            get
            {
                return m_maxConnectionAttempts;
            }
            set
            {
                if (value < 1)
                    m_maxConnectionAttempts = -1;
                else
                    m_maxConnectionAttempts = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the client will do a handshake with the server after the connection has been established.
        /// </summary>
        /// <remarks>
        /// <see cref="Handshake"/> is required when <see cref="SecureSession"/> is enabled.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Handshake"/> is being disabled while <see cref="SecureSession"/> is enabled.</exception>
        [Category("Security"),
        DefaultValue(DefaultHandshake),
        Description("Indicates whether the client will do a handshake with the server after accepting its connection.")]
        public virtual bool Handshake
        {
            get
            {
                return m_handshake;
            }
            set
            {
                // Can't disable handshake when secure session is enabled.
                if (!value && m_secureSession)
                    throw new InvalidOperationException("Handshake is required when SecureSession is enabled.");

                m_handshake = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds that the client will wait for the server's response to the <see cref="Handshake"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The value specified is either zero or negative.</exception>
        [Category("Security"),
        DefaultValue(DefaultHandshakeTimeout),
        Description("The number of milliseconds that the client will wait for the server's response to the Handshake.")]
        public virtual int HandshakeTimeout
        {
            get
            {
                return m_handshakeTimeout;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentException("Value cannot be zero or negative.");

                m_handshakeTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the passpharse that the client will provide for authentication during the <see cref="Handshake"/> process.
        /// </summary>
        [Category("Security"),
        DefaultValue(DefaultHandshakePassphrase),
        Description("The passpharse that the client will provide for authentication during the Handshake process.")]
        public virtual string HandshakePassphrase
        {
            get
            {
                return m_handshakePassphrase;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    m_handshakePassphrase = value;
                else
                    m_handshakePassphrase = DefaultHandshakePassphrase;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the data exchanged between the client and server will be encrypted using a private session passphrase.
        /// </summary>
        ///<remarks>
        ///<see cref="Handshake"/> and <see cref="Encryption"/> must be enabled in order to use <see cref="SecureSession"/>.
        ///</remarks>
        ///<exception cref="InvalidOperationException"><see cref="SecureSession"/> is being enabled before enabling <see cref="Handshake"/>.</exception>
        ///<exception cref="InvalidOperationException"><see cref="SecureSession"/> is being enabled before enabling <see cref="Encryption"/>.</exception>
        [Category("Security"),
        DefaultValue(DefaultSecureSession),
        Description("Indicates whether the data exchanged between the client and server will be encrypted using a private session passphrase.")]
        public virtual bool SecureSession
        {
            get
            {
                return m_secureSession;
            }
            set
            {
                // Handshake is required for SecureSession.
                if (value && !m_handshake)
                    throw new InvalidOperationException("Handshake must be enabled in order to use SecureSession.");

                // Encryption is required for SecureSession.
                if (value && m_encryption == CipherStrength.None)
                    throw new InvalidOperationException("Encryption must be enabled in order to use SecureSession.");

                m_secureSession = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of milliseconds after which the client will raise the <see cref="ReceiveDataTimeout"/> event if no data is received from the server.
        /// </summary>
        /// <remarks>Set <see cref="ReceiveTimeout"/> to -1 to disable this feature.</remarks>
        [Category("Data"),
        DefaultValue(DefaultReceiveTimeout),
        Description("The number of milliseconds after which the client will raise the ReceiveClientDataTimedout event if no data is received from the server.")]
        public virtual int ReceiveTimeout
        {
            get
            {
                return m_receiveTimeout;
            }
            set
            {
                if (value < 1)
                    m_receiveTimeout = -1;
                else
                    m_receiveTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the size of the buffer used by the client for receiving data from the server.
        /// </summary>
        /// <exception cref="ArgumentException">The value specified is either zero or negative.</exception>
        [Category("Data"),
        DefaultValue(DefaultReceiveBufferSize),
        Description("The size of the buffer used by the client for receiving data from the server.")]
        public virtual int ReceiveBufferSize
        {
            get
            {
                return m_receiveBufferSize;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentException("Value cannot be zero or negative.");

                m_receiveBufferSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CipherStrength"/> to be used for ciphering the data exchanged between the client and server.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///     <listheader>
        ///         <term><see cref="SecureSession"/></term>
        ///         <description>Key used for <see cref="Encryption"/></description>
        ///     </listheader>
        ///     <item>
        ///         <term>Disabled</term>
        ///         <description><see cref="HandshakePassphrase"/> is used.</description>
        ///     </item>
        ///     <item>
        ///         <term>Enabled</term>
        ///         <description>A private key exchanged between the client and server during the <see cref="Handshake"/> process is used.</description>
        ///     </item>
        /// </list>
        /// </remarks>
        [Category("Data"),
        DefaultValue(DefaultEncryption),
        Description("The CipherStrength to be used for ciphering the data exchanged between the client and server.")]
        public virtual CipherStrength Encryption
        {
            get
            {
                return m_encryption;
            }
            set
            {
                // Can't disable encryption when secure session is enabled.
                if (value == CipherStrength.None && m_secureSession)
                    throw new InvalidOperationException("Encryption is required when SecureSession is enabled.");

                m_encryption = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CompressionStrength"/> to be used for compressing the data exchanged between the client and server.
        /// </summary>
        [Category("Data"),
        DefaultValue(DefaultCompression),
        Description("The CompressionStrength to be used for compressing the data exchanged between the client and server.")]
        public virtual CompressionStrength Compression
        {
            get
            {
                return m_compression;
            }
            set
            {
                m_compression = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the client settings are to be saved to the config file.
        /// </summary>
        [Category("Persistance"),
        DefaultValue(DefaultPersistSettings),
        Description("Indicates whether the client settings are to be saved to the config file.")]
        public bool PersistSettings
        {
            get
            {
                return m_persistSettings;
            }
            set
            {
                m_persistSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the category under which the client settings are to be saved to the config file if the <see cref="PersistSettings"/> property is set to true.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value specified is a null or empty string.</exception>
        [Category("Persistance"),
        DefaultValue(DefaultSettingsCategory),
        Description("Category under which the client settings are to be saved to the config file if the PersistSettings property is set to true.")]
        public string SettingsCategory
        {
            get
            {
                return m_settingsCategory;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw (new ArgumentNullException());

                m_settingsCategory = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the client is currently enabled.
        /// </summary>
        /// <remarks>
        /// Setting <see cref="Enabled"/> to true will start connection cycle for the client if it
        /// is not connected, setting to false will disconnect the client if it is connected.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Enabled
        {
            get
            {
                return m_currentState == ClientState.Connected;
            }
            set
            {
                if (value && m_currentState == ClientState.Disconnected)
                    Connect();
                else if (!value && m_currentState != ClientState.Disconnected)
                    Disconnect();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> to be used for the text sent to the server.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Encoding TextEncoding
        {
            get
            {
                return m_textEncoding;
            }
            set
            {
                m_textEncoding = value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="Delegate"/> to be invoked instead of the <see cref="ReceiveDataComplete"/> event when data is received from server.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property only needs to be implemented if you need data from the server absolutelty as fast as possible, for most uses this will not be necessary.  
        /// Setting this property gives the consumer access to the data stream as soon as it's available, but this also bypasses <see cref="Encryption"/> and 
        /// <see cref="Compression"/> on received data.
        /// </para>
        /// <para>
        /// arg1 in <see cref="ReceiveDataHandler"/> is the buffer containing the data received from the server.<br/>
        /// arg2 in <see cref="ReceiveDataHandler"/> is the zero based starting offset into the buffer containing the data received from the server.<br/>
        /// arg3 in <see cref="ReceiveDataHandler"/> is the number of bytes received from the server that is stored in the buffer (arg1) starting at index 0.
        /// </para>
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Action<byte[], int, int> ReceiveDataHandler
        {
            get
            {
                return m_receiveDataHandler;
            }
            set
            {
                m_receiveDataHandler = value;
            }
        }

        /// <summary>
        /// Gets the server ID.
        /// </summary>
        /// <remarks>
        /// <see cref="ServerID"/> will be <see cref="Guid.Empty"/> when <see cref="Handshake"/> is disabled.
        /// </remarks>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Guid ServerID
        {
            get
            {
                return m_serverID;
            }
            set
            {
                m_serverID = value;
            }
        }

        /// <summary>
        /// Gets the client ID.
        /// </summary>
        [Browsable(false),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Guid ClientID
        {
            get
            {
                return m_clientID;
            }
            set
            {
                m_clientID = value;
            }
        }

        /// <summary>
        /// Gets the current <see cref="ClientState"/>.
        /// </summary>
        public ClientState CurrentState
        {
            get
            {
                return m_currentState;
            }
        }

        /// <summary>
        /// Gets the <see cref="TransportProtocol"/> used by the client for the transportation of data with the server.
        /// </summary>
        [Browsable(false)]
        public virtual TransportProtocol TransportProtocol
        {
            get
            {
                return m_transportProtocol;
            }
        }

        /// <summary>
        /// Gets the total number of seconds for which the client has been connected to the server.
        /// </summary>
        [Browsable(false)]
        public virtual Time ConnectionTime
        {
            get
            {
                Time clientConnectionTime = 0.0D;

                if (m_connectTime > 0)
                {
                    if (m_currentState == ClientState.Connected)
                        // Client is connected to the server.
                        clientConnectionTime = Ticks.ToSeconds(DateTime.Now.Ticks - m_connectTime);
                    else
                        // Client is not connected to the server.
                        clientConnectionTime = Ticks.ToSeconds(m_disconnectTime - m_connectTime);
                }

                return clientConnectionTime;
            }
        }

        /// <summary>
        /// Gets the unique identifier of the client.
        /// </summary>
        [Browsable(false)]
        public virtual string Name
        {
            get
            {
                return m_settingsCategory;
            }
        }

        /// <summary>
        /// Gets the descriptive status of the client.
        /// </summary>
        [Browsable(false)]
        public virtual string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();
                if (m_handshake)
                {
                    // Display ID only if handshaking is enabled.
                    status.Append("                 Server ID: ");
                    status.Append(m_serverID.ToString());
                    status.AppendLine();
                    status.Append("                 Client ID: ");
                    status.Append(m_clientID.ToString());
                    status.AppendLine();
                }
                status.Append("              Client state: ");
                status.Append(m_currentState);
                status.AppendLine();
                status.Append("           Connection time: ");
                status.Append(ConnectionTime.ToString());
                status.AppendLine();
                status.Append("            Receive buffer: ");
                status.Append(m_receiveBufferSize.ToString());
                status.AppendLine();
                status.Append("        Transport protocol: ");
                status.Append(m_transportProtocol.ToString());
                status.AppendLine();
                status.Append("        Text encoding used: ");
                status.Append(m_textEncoding.EncodingName);
                status.AppendLine();

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        #region [ Abstract ]

        /// <summary>
        /// When overridden in a derived class, disconnects client from the server synchronously.
        /// </summary>
        public abstract void Disconnect();

        /// <summary>
        /// When overridden in a derived class, connects client to the server asynchronously.
        /// </summary>
        public abstract void ConnectAsync();

        /// <summary>
        /// When overridden in a derived class, validates the specified <paramref name="connectionString"/>.
        /// </summary>
        /// <param name="connectionString">The connection string to be validated.</param>
        protected abstract void ValidateConnectionString(string connectionString);

        /// <summary>
        /// When overridden in a derived class, returns the passphrase used for ciphering data.
        /// </summary>
        protected abstract string GetSessionPassphrase();

        /// <summary>
        /// When overridden in a derived class, sends data to the server asynchronously.
        /// </summary>
        /// <param name="data">The buffer that contains the binary data to be sent.</param>
        /// <param name="offset">The zero-based position in the <paramref name="data"/> at which to begin sending data.</param>
        /// <param name="length">The number of bytes to be sent from <paramref name="data"/> starting at the <paramref name="offset"/>.</param>
        /// <returns><see cref="WaitHandle"/> for the asynchronous operation.</returns>
        protected abstract WaitHandle SendDataAsync(byte[] data, int offset, int length);

        #endregion

        /// <summary>
        /// Initializes the client.
        /// </summary>
        /// <remarks>
        /// <see cref="Initialize()"/> is to be called by user-code directly only if the client is not consumed through the designer surface of the IDE.
        /// </remarks>
        public void Initialize()
        {
            if (!m_initialized)
            {
                LoadSettings();         // Load settings from the config file.
                m_initialized = true;   // Initialize only once.
            }
        }

        /// <summary>
        /// Connects the client to the server synchronously.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="MaxConnectionAttempts"/> is set to -1.</exception>
        public void Connect()
        {
            // Don't allow blocking connection to be attempted when MaxConnectionAttempts = -1.
            if (MaxConnectionAttempts == -1)
                throw new InvalidOperationException("Synchronous connection cannot be attempted with MaxConnectionAttempts = -1.");

            // Start asynchronous connection attempt.
            ConnectAsync();
            // Block for connection process to complete.
            while (m_currentState != ClientState.Connected)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Sends data to the server synchronously.
        /// </summary>
        /// <param name="data">The plain-text data that is to be sent.</param>
        public virtual void Send(string data)
        {
            Send(m_textEncoding.GetBytes(data));
        }

        /// <summary>
        /// Sends data to the server synchronously.
        /// </summary>
        /// <param name="serializableObject">The serializable object that is to be sent.</param>
        public virtual void Send(object serializableObject)
        {
            Send(Serialization.GetBytes(serializableObject));
        }

        /// <summary>
        /// Sends data to the server synchronously.
        /// </summary>
        /// <param name="data">The binary data that is to be sent.</param>
        public virtual void Send(byte[] data)
        {
            Send(data, 0, data.Length);
        }

        /// <summary>
        /// Sends data to the server synchronously.
        /// </summary>
        /// <param name="data">The buffer that contains the binary data to be sent.</param>
        /// <param name="offset">The zero-based position in the <paramref name="data"/> at which to begin sending data.</param>
        /// <param name="length">The number of bytes to be sent from <paramref name="data"/> starting at the <paramref name="offset"/>.</param>
        public virtual void Send(byte[] data, int offset, int length)
        {
            SendAsync(data, offset, length).WaitOne();
        }

        /// <summary>
        /// Saves client settings to the config file if the <see cref="PersistSettings"/> property is set to true.
        /// </summary>        
        public virtual void SaveSettings()
        {
            if (m_persistSettings)
            {
                // Ensure that settings category is specified.
                if (string.IsNullOrEmpty(m_settingsCategory))
                    throw new InvalidOperationException("SettingsCategory property has not been set.");

                // Save settings under the specified category.
                ConfigurationFile config = ConfigurationFile.Current;
                CategorizedSettingsElementCollection settings = config.Settings[m_settingsCategory];
                settings["ConnectionString", true].Update(m_connectionString, "Data required by the client to connect to the server.");
                settings["ReceiveBufferSize", true].Update(m_receiveBufferSize, "Maximum number of bytes that can be received at a time by the client from the server.");
                settings["ReceiveTimeout", true].Update(m_receiveTimeout, "Time to wait in milliseconds for data to be received from the server before timing out.");
                settings["MaximumConnectionAttempts", true].Update(m_maxConnectionAttempts, "Maximum number of times the client will attempt to connect to the server.");
                settings["SecureSession", true].Update(m_secureSession, "True if the data exchanged between the client and server will be encrypted using a private session passphrase; otherwise False.");
                settings["Handshake", true].Update(m_handshake, "True if the client will do a handshake with the server; otherwise False.");
                settings["HandshakePassphrase", true].Update(m_handshakePassphrase, "Passpharse that will provided to the server for authentication during the handshake process.");
                settings["Encryption", true].Update(m_encryption, "Cipher strength (None; Level1; Level2; Level3; Level4; Level5) to be used for encrypting the data exchanged between the client and server.");
                settings["Compression", true].Update(m_compression, "Compression strength (NoCompression; DefaultCompression; BestSpeed; BestCompression; MultiPass) to be used for compressing the data exchanged between the client and server.");
                config.Save();
            }
        }

        /// <summary>
        /// Loads saved client settings from the config file if the <see cref="PersistSettings"/> property is set to true.
        /// </summary>        
        public virtual void LoadSettings()
        {
            if (m_persistSettings)
            {
                // Ensure that settings category is specified.
                if (string.IsNullOrEmpty(m_settingsCategory))
                    throw new InvalidOperationException("SettingsCategory property has not been set.");

                // Load settings from the specified category.
                ConfigurationFile config = ConfigurationFile.Current;
                CategorizedSettingsElementCollection settings = config.Settings[m_settingsCategory];
                ConnectionString = settings["ConnectionString", true].ValueAs(m_connectionString);
                MaxConnectionAttempts = settings["MaximumConnectionAttempts", true].ValueAs(m_maxConnectionAttempts);
                Handshake = settings["Handshake", true].ValueAs(m_handshake);
                HandshakePassphrase = settings["HandshakePassphrase", true].ValueAs(m_handshakePassphrase);
                ReceiveTimeout = settings["ReceiveTimeout", true].ValueAs(m_receiveTimeout);
                ReceiveBufferSize = settings["ReceiveBufferSize", true].ValueAs(m_receiveBufferSize);
                Encryption = settings["Encryption", true].ValueAs(m_encryption);
                Compression = settings["Compression", true].ValueAs(m_compression);
                SecureSession = settings["SecureSession", true].ValueAs(m_secureSession);
            }
        }

        /// <summary>
        /// Performs necessary operations before the client properties are initialized.
        /// </summary>
        /// <remarks>
        /// <see cref="BeginInit()"/> should never be called by user-code directly. This method exists solely for use by the designer if the server is consumed through 
        /// the designer surface of the IDE.
        /// </remarks>
        public void BeginInit()
        {
            // Nothing needs to be done before component is initialized.
        }

        /// <summary>
        /// Performs necessary operations after the client properties are initialized.
        /// </summary>
        /// <remarks>
        /// <see cref="EndInit()"/> should never be called by user-code directly. This method exists solely for use by the designer if the server is consumed through the 
        /// designer surface of the IDE.
        /// </remarks>
        public void EndInit()
        {
            if (!DesignMode)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Sends data to the server asynchronously.
        /// </summary>
        /// <param name="data">The plain-text data that is to be sent.</param>
        /// <returns><see cref="WaitHandle"/> for the asynchronous operation.</returns>
        public virtual WaitHandle SendAsync(string data)
        {
            return SendAsync(m_textEncoding.GetBytes(data));
        }

        /// <summary>
        /// Sends data to the server asynchronously.
        /// </summary>
        /// <param name="serializableObject">The serializable object that is to be sent.</param>
        /// <returns><see cref="WaitHandle"/> for the asynchronous operation.</returns>
        public virtual WaitHandle SendAsync(object serializableObject)
        {
            return SendAsync(Serialization.GetBytes(serializableObject));
        }

        /// <summary>
        /// Sends data to the server asynchronously.
        /// </summary>
        /// <param name="data">The binary data that is to be sent.</param>
        /// <returns><see cref="WaitHandle"/> for the asynchronous operation.</returns>
        public virtual WaitHandle SendAsync(byte[] data)
        {
            return SendAsync(data, 0, data.Length);
        }

        /// <summary>
        /// Sends data to the server asynchronously.
        /// </summary>
        /// <param name="data">The buffer that contains the binary data to be sent.</param>
        /// <param name="offset">The zero-based position in the <paramref name="data"/> at which to begin sending data.</param>
        /// <param name="length">The number of bytes to be sent from <paramref name="data"/> starting at the <paramref name="offset"/>.</param>
        /// <returns><see cref="WaitHandle"/> for the asynchronous operation.</returns>
        public virtual WaitHandle SendAsync(byte[] data, int offset, int length)
        {
            if (m_currentState == ClientState.Connected)
            {
                // Pre-condition data as needed and then send it.
                Payload.ProcessTransmit(ref data, ref offset, ref length, m_encryption, GetSessionPassphrase(), m_compression);
                return SendDataAsync(data, offset, length);
            }
            else
            {
                throw new InvalidOperationException("Client is not connected.");
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the client and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    // This will be done regardless of whether the object is finalized or disposed.
                    SaveSettings();
                    if (disposing)
                    {
                        // This will be done only when the object is disposed by calling Dispose().
                        Disconnect();
                    }
                }
                finally
                {
                    base.Dispose(disposing);    // Call base class Dispose().
                    m_disposed = true;          // Prevent duplicate dispose.
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ConnectionAttempt"/> event.
        /// </summary>
        protected virtual void OnConnectionAttempt()
        {
            m_currentState = ClientState.Connecting;

            if (ConnectionAttempt != null)
                ConnectionAttempt(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="ConnectionEstablished"/> event.
        /// </summary>
        protected virtual void OnConnectionEstablished()
        {
            m_currentState = ClientState.Connected;
            m_disconnectTime = 0;
            m_connectTime = DateTime.Now.Ticks;     // Save the time when the client connected to the server.

            if (ConnectionEstablished != null)
                ConnectionEstablished(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="ConnectionTerminated"/> event.
        /// </summary>
        protected virtual void OnConnectionTerminated()
        {
            m_currentState = ClientState.Disconnected;
            m_serverID = Guid.Empty;
            m_disconnectTime = DateTime.Now.Ticks;  // Save the time when client was disconnected from the server.

            if (ConnectionTerminated != null)
                ConnectionTerminated(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="ConnectionException"/> event.
        /// </summary>
        /// <param name="ex">Exception to send to <see cref="ConnectionException"/> event.</param>
        protected virtual void OnConnectionException(Exception ex)
        {
            if (ConnectionException != null)
                ConnectionException(this, new EventArgs<Exception>(ex));
        }

        /// <summary>
        /// Raises the <see cref="HandshakeProcessTimeout"/> event.
        /// </summary>
        protected virtual void OnHandshakeProcessTimeout()
        {
            if (HandshakeProcessTimeout != null)
                HandshakeProcessTimeout(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="HandshakeProcessUnsuccessful"/> event.
        /// </summary>
        protected virtual void OnHandshakeProcessUnsuccessful()
        {
            if (HandshakeProcessUnsuccessful != null)
                HandshakeProcessUnsuccessful(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="SendDataStart"/> event.
        /// </summary>
        protected virtual void OnSendDataStart()
        {
            if (SendDataStart != null)
                SendDataStart(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="SendDataComplete"/> event.
        /// </summary>
        protected virtual void OnSendDataComplete()
        {
            if (SendDataComplete != null)
                SendDataComplete(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="SendDataException"/> event.
        /// </summary>
        /// <param name="ex">Exception to send to <see cref="SendDataException"/> event.</param>
        protected virtual void OnSendDataException(Exception ex)
        {
            if (SendDataException != null)
                SendDataException(this, new EventArgs<Exception>(ex));
        }

        /// <summary>
        /// Raises the <see cref="ReceiveDataTimeout"/> event.
        /// </summary>
        protected virtual void OnReceiveDataTimeout()
        {
            if (ReceiveDataTimeout != null)
                ReceiveDataTimeout(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="ReceiveDataComplete"/> event.
        /// </summary>
        /// <param name="data">Data received from the client.</param>
        /// <param name="size">Number of bytes received from the client.</param>
        protected virtual void OnReceiveDataComplete(byte[] data, int size)
        {
            if (m_receiveDataHandler != null)
            {
                m_receiveDataHandler(data, 0, size);
            }
            else
            {
                if (ReceiveDataComplete != null)
                {
                    try
                    {
                        int offset = 0; // Received buffer will always have valid data starting at offset zero.
                        Payload.ProcessReceived(ref data, ref offset, ref size, m_encryption, GetSessionPassphrase(), m_compression);
                    }
                    catch
                    {
                        // Ignore encountered exception and pass-on the raw data.
                    }
                    finally
                    {
                        ReceiveDataComplete(this, new EventArgs<byte[], int>(data, size));
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="ReceiveDataException"/> event.
        /// </summary>
        /// <param name="ex">Exception to send to <see cref="ReceiveDataException"/> event.</param>
        protected virtual void OnReceiveDataException(Exception ex)
        {
            if (ReceiveDataException != null)
                ReceiveDataException(this, new EventArgs<Exception>(ex));
        }

        #endregion

        #region [ Static ]

        /// <summary>
        /// Create a communications client
        /// </summary>
        /// <remarks>
        /// Note that typical connection string should be prefixed with a "protocol=tcp", "protocol=udp", "protocol=serial" or "protocol=file"
        /// </remarks>
        public static IClient Create(string connectionString)
        {
            Dictionary<string, string> connectionData = connectionString.ParseKeyValuePairs();
            IClient client = null;
            string protocol;

            if (connectionData.TryGetValue("protocol", out protocol))
            {
                connectionData.Remove("protocol");
                StringBuilder settings = new StringBuilder();

                foreach (string key in connectionData.Keys)
                {
                    settings.Append(key);
                    settings.Append("=");
                    settings.Append(connectionData[key]);
                    settings.Append(";");
                }

                switch (protocol.ToLower())
                {
                    case "tcp":
                        client = new TcpClient(settings.ToString());
                        break;
                    case "udp":
                        client = new UdpClient(settings.ToString());
                        break;
                    case "file":
                        client = new FileClient(settings.ToString());
                        break;
                    case "serial":
                        client = new SerialClient(settings.ToString());
                        break;
                    default:
                        throw new ArgumentException(protocol + " is not a valid transport protocol.");
                }
            }
            else
            {
                throw new ArgumentException("Transport protocol must be specified.");
            }

            return client;
        }

        #endregion
    }
}