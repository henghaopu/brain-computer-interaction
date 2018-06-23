using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class InteractionWithMuse : MonoBehaviour {

    // Public members that connects to UI components
    public Button startScanButton;
    public Button connectButton;
    public Button disconnectButton;
    public Dropdown museList;
    public Text dataText;
    public Text connectionText;
    // Text
    public Text thetaScoreText;
    public Text alphaScoreText;
    public Text betaScoreText;
    public Text betaOverThetaText;
    // Slider
    public Slider betaOverTheta1;
    public Slider betaOverTheta2;
    public Slider betaOverTheta3;
    public Slider betaOverTheta4;
    public Slider betaOverTheta5;
    public Slider betaOverTheta6;
    public Slider betaOverThetaAvg;

    // Public methods that gets called on UI events.
    public void startScanning() {
        // Must register at least MuseListeners before scanning for headbands.
        // Otherwise no callbacks will be triggered to get a notification.
        muse.startListening();
    }

    public void userSelectedMuse() {
        userPickedMuse = museList.options [museList.value].text;
        Debug.Log ("Selected muse = " + userPickedMuse);
    }

    public void connect() {        
        // If user just clicks connect without selecting a muse from the
        // dropdown menu, then connect to the one displayed in the dropdown.
        if (userPickedMuse == "") {
            userPickedMuse = museList.options [0].text;
        }
        Debug.Log ("Connecting to " + userPickedMuse);
        muse.connect (userPickedMuse);
    }

    public void disconnect() {
        muse.disconnect ();
    }

    // Private Members
    private string userPickedMuse;
    private string dataBuffer;
    private string connectionBuffer;
    private LibmuseBridge muse;

    private string thetaBuffer;
    private string alphaBuffer;
    private string betaBuffer;
    private string betaOverThetaBuffer;
    private float[] betaOverThetaValues;

    // Private Methods

    // Use this for initialization
    void Start () {

#if UNITY_IPHONE
        muse = new LibmuseBridgeIos();
#elif UNITY_ANDROID
        muse = new LibmuseBridgeAndroid();
#endif
        Debug.Log("Libmuse version = " + muse.getLibmuseVersion());

        userPickedMuse = "";
        dataBuffer = "";
        connectionBuffer = "";
        registerListeners();
        registerAllData();

        thetaBuffer = "";
        alphaBuffer = "";
        betaBuffer = "";
        betaOverThetaBuffer = "";
        betaOverThetaValues = new float[] { 0, 0, 0, 0, 0, 0, 0 };
    }


    void registerListeners() {
        muse.registerMuseListener(this.name, "receiveMuseList");
        muse.registerConnectionListener(this.name, "receiveConnectionPackets");
        muse.registerDataListener(this.name, "receiveDataPackets");
        muse.registerArtifactListener(this.name, "receiveArtifactPackets");
    }

    void registerAllData() {
        // This will register for all the available data from muse headband
        // Comment out the ones you don't want
        muse.listenForDataPacket("ACCELEROMETER");
        muse.listenForDataPacket("GYRO");
        muse.listenForDataPacket("EEG");
        muse.listenForDataPacket("QUANTIZATION");
        muse.listenForDataPacket("BATTERY");
        muse.listenForDataPacket("DRL_REF");
        muse.listenForDataPacket("ALPHA_ABSOLUTE");
        muse.listenForDataPacket("BETA_ABSOLUTE");
        muse.listenForDataPacket("DELTA_ABSOLUTE");
        muse.listenForDataPacket("THETA_ABSOLUTE");
        muse.listenForDataPacket("GAMMA_ABSOLUTE");
        muse.listenForDataPacket("ALPHA_RELATIVE");
        muse.listenForDataPacket("BETA_RELATIVE");
        muse.listenForDataPacket("DELTA_RELATIVE");
        muse.listenForDataPacket("THETA_RELATIVE");
        muse.listenForDataPacket("GAMMA_RELATIVE");
        muse.listenForDataPacket("ALPHA_SCORE");
        muse.listenForDataPacket("BETA_SCORE");
        muse.listenForDataPacket("DELTA_SCORE");
        muse.listenForDataPacket("THETA_SCORE");
        muse.listenForDataPacket("GAMMA_SCORE");
        muse.listenForDataPacket("HSI_PRECISION");
        muse.listenForDataPacket("ARTIFACTS");
    }

    // These listener methods update the buffer
    // The Update() per frame will display the data.
    void receiveMuseList(string data) {
        // This method will receive a list of muses delimited by white space.
        Debug.Log("Found list of muses = " + data);

        // Convert string to list of muses and populate the dropdown menu.
        List<string> muses = data.Split(' ').ToList<string>();
        museList.ClearOptions ();
        museList.AddOptions (muses);
    }

    void receiveConnectionPackets(string data) {
        Debug.Log("Unity received connection packet: " + data);
        connectionBuffer = data;
    }

    // Heng-Hao 
    void receiveDataPackets(string data) {   
        Debug.Log("Unity received data packet: " + data);
        dataBuffer = data;

        // 
        if (dataBuffer != "") 
        {
            string[] separators = { "{\"DataPacketType\":\"", "\",\"DataPacketValue\":", ",\"TimeStamp\":", "}" };
            string[] infos = dataBuffer.Split(separators, StringSplitOptions.None);

            switch (infos[1]) 
            {
                case "THETA_SCORE":
                    thetaBuffer = infos[2];
                    break;
                case "ALPHA_SCORE":
                    alphaBuffer = infos[2];
                    break;
                case "BETA_SCORE":
                    betaBuffer = infos[2];
                    break;
            }

            // if the Data Packets have Values 
            if (thetaBuffer != "" && betaBuffer != "")
            {
                char[] delimiters = { '[', ',', ']' };
                string[] thetaValues = thetaBuffer.Split(delimiters);
                string[] betaValues = betaBuffer.Split(delimiters);
                betaOverThetaBuffer = "";  // restart a buffer
                for (int i = 1; i < (thetaValues.Length - 1); i++)
                {
                    if (thetaValues[i] == "0" || thetaValues[i] == "0.00")
                    {
                        betaOverThetaBuffer += (thetaValues[i] + ", ");
                        betaOverThetaValues[i - 1] = Convert.ToSingle(thetaValues[i]);
                    }
                    else
                    {
                        float temp = (Convert.ToSingle(betaValues[i]) / Convert.ToSingle(thetaValues[i]));
                        betaOverThetaBuffer += (temp.ToString("F2") + ", ");
                        if (temp >= 5)
                        {
                            betaOverThetaValues[i - 1] = 5;
                        }
                        else
                        {
                            betaOverThetaValues[i - 1] = temp;
                        }
                    }

                    switch (i)
                    {
                        case 1:
                            betaOverTheta1.value = betaOverThetaValues[i - 1];
                            break;
                        case 2:
                            betaOverTheta2.value = betaOverThetaValues[i - 1];
                            break;
                        case 3:
                            betaOverTheta3.value = betaOverThetaValues[i - 1];
                            break;
                        case 4:
                            betaOverTheta4.value = betaOverThetaValues[i - 1];
                            break;
                        case 5:
                            betaOverTheta5.value = betaOverThetaValues[i - 1];
                            break;
                        case 6:
                            betaOverTheta6.value = betaOverThetaValues[i - 1];
                            break;
                    }
                    betaOverThetaAvg.value = (betaOverTheta1.value + betaOverTheta2.value + betaOverTheta3.value + betaOverTheta4.value + betaOverTheta5.value + betaOverTheta6.value) / 6;
                }
            }
        }
    }

    void receiveArtifactPackets(string data) {
        Debug.Log("Unity received artifact packet: " + data);
        dataBuffer = data;
    }
    
    // Update is called once per frame
    void Update () {
        // Display the data in the UI Text field
        dataText.text = dataBuffer;
        connectionText.text = connectionBuffer;

        thetaScoreText.text = thetaBuffer;
        alphaScoreText.text = alphaBuffer;
        betaScoreText.text = betaBuffer;

        betaOverThetaText.text = betaOverThetaBuffer;
    }
}