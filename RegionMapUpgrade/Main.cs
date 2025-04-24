using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using RumbleModdingAPI;
using System.Collections;
using Il2CppRUMBLE.Networking.MatchFlow.Regions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppRUMBLE.Networking.MatchFlow.Regions.Interactions;
using HarmonyLib;
using Il2CppRUMBLE.Managers;
using RumbleModUI;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.UI;

namespace RegionMapUpgrade
{
    public static class BuildInfo
    {
        public const string ModName = "RegionMapUpgrade";
        public const string ModVersion = "1.1.3";
        public const string Description = "";
        public const string Author = "UlvakSkillz";
        public const string Company = "";
    }

    [RegisterTypeInIl2Cpp]
    public class HandColliderMapCheck : MonoBehaviour
    {
        private bool thisOneIsTouching = false;

        void OnTriggerEnter(Collider other)
        {
            if (Main.touchingARegion || !Main.touchscreen || Main.running || ((other.gameObject.name != "Bone_HandAlpha_L") && (other.gameObject.name != "Bone_HandAlpha_R")))
            {
                return;
            }
            Main.touchingARegion = true;
            thisOneIsTouching = true;
            this.gameObject.GetComponent<Region>().Select();
        }

        void OnTriggerExit(Collider other)
        {
            if (!thisOneIsTouching || ((other.gameObject.name != "Bone_HandAlpha_L") && (other.gameObject.name != "Bone_HandAlpha_R")))
            {
                return;
            }
            Main.touchingARegion = false;
            thisOneIsTouching = false;
        }
    }

    public class Main : MelonMod
    {
        private string currentScene = "Loader";
        private Mod RegionMapUpgrade = new Mod();
        public static List<bool> enabledRegions = new List<bool>() { false, false, false, false, false, false, false, false, false, false, false, false, false };
        private Shader urp;
        public static bool running = false;
        public static bool touchscreen = true;
        public static bool mapColor = true;
        public static bool mapColorGrey = true;
        public static int greenMin = 9;
        public static int yellowMin = 1;
        public static bool showPing = true;

        private bool bail = false;
        private bool requeueing = false;
        public static bool touchingARegion = false;
        private List<GameObject> regionCountText = new List<GameObject>();
        private RegionSelector regionSelector;
        private Il2CppReferenceArray<Region> regions;
        public static List<string> regionCodes = new List<string>() { "asia", "au", "cae", "eu", "in", "jp", "ru", "rue", "za", "sa", "kr", "us", "usw" };
        public static List<string> regionTextGOOrder = new List<string>() { "us", "usw", "eu", "au", "cae", "asia", "in", "jp", "ru", "rue", "za", "sa", "kr" };
        public static string[] regionTitles = new string[13] { "Asia", "Australia", "Canada", "Europe", "India", "Japan", "Russia", "Russia, East", "South Africa", "South America", "South Korea", "USA, East", "USA, West" };
        public static List<int> regionOrder = new List<int>() { 11, 12, 3, 1, 2, 0, 4, 5, 6, 7, 8, 9, 10 };
        private float[] prevStepDurations;
        private RevolvingNumberCollection pingRef, playerRef;
        private Shader regionGlow;

        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        public override void OnLateInitializeMelon()
        {
            RegionMapUpgrade.ModName = BuildInfo.ModName;
            RegionMapUpgrade.ModVersion = BuildInfo.ModVersion;
            RegionMapUpgrade.SetFolder("RegionMapUpgrade");
            RegionMapUpgrade.AddToList("USA, East", true, 0, "Toggle's US East Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("USA, West", true, 0, "Toggle's US West Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Europe", true, 0, "Toggle's Europe Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Australia", true, 0, "Toggle's Australia Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Canada", true, 0, "Toggle's Canada Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Asia", true, 0, "Toggle's Asia Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("India", true, 0, "Toggle's India Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Japan", true, 0, "Toggle's Japan Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Russia ", true, 0, "Toggle's Russia Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Russia, East", true, 0, "Toggle's Russia, East Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South Africa", true, 0, "Toggle's South Africa Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South America", true, 0, "Toggle's South America Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South Korea", true, 0, "Toggle's South Korea Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Touch Screen", true, 0, "Toggle's TouchScreen Version of RegionBoard On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Map Color  ", true, 0, "Toggle's Coloring the World Map On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Map Color - Grey", true, 0, "Toggle's Grey Coloring the World Map On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Map Color - Green Min", 9, "Sets the Minimum Player Count needed to Turn Green on the World Map", new Tags { });
            RegionMapUpgrade.AddToList("Map Color - Yellow Min", 1, "Sets the Minimum Player Count needed to Turn Yellow on the World Map", new Tags { });
            RegionMapUpgrade.AddToList("Show Ping", true, 0, "Toggle's Ping Counter On/Off", new Tags { });
            RegionMapUpgrade.GetFromFile();
            Save();
            Calls.onMapInitialized += mapInit;
            Calls.onMatchStarted += AddButtonListeners;
            UI.instance.UI_Initialized += UIInit;
            RegionMapUpgrade.ModSaved += Save;
            urp = Shader.Find("Universal Render Pipeline/Lit");
            regionGlow = Shader.Find("Shader Graphs/RegionSelectGlow");
        }

        private void UIInit()
        {
            UI.instance.AddMod(RegionMapUpgrade);
        }

        private void Save()
        {
            for (int i = 0; i < enabledRegions.Count; i++)
            {
                enabledRegions[i] = (bool)RegionMapUpgrade.Settings[i].SavedValue;
            }
            bool pastTouchscreen = touchscreen;
            touchscreen = (bool)RegionMapUpgrade.Settings[enabledRegions.Count].SavedValue;
            bool pastMapColor = mapColor;
            mapColor = (bool)RegionMapUpgrade.Settings[enabledRegions.Count + 1].SavedValue;
            bool pastMapColorGrey = mapColorGrey;
            mapColorGrey = (bool)RegionMapUpgrade.Settings[enabledRegions.Count + 2].SavedValue;
            int pastGreenMin = greenMin;
            greenMin = (int)RegionMapUpgrade.Settings[enabledRegions.Count + 3].SavedValue;
            int pastYellowMin = yellowMin;
            yellowMin = (int)RegionMapUpgrade.Settings[enabledRegions.Count + 4].SavedValue;
            bool pastShowPing = showPing;
            showPing = (bool)RegionMapUpgrade.Settings[enabledRegions.Count + 5].SavedValue;
            if ((greenMin < yellowMin) || (yellowMin < 1) || (greenMin < 2))
            {
                Log("Error Setting Green/Yellow Minimums. Reverting Values. Values Given: Green: " + greenMin + ", Yellow: " + yellowMin);
                greenMin = pastGreenMin;
                yellowMin = pastYellowMin;
                RegionMapUpgrade.Settings[enabledRegions.Count + 3].SavedValue = greenMin;
                RegionMapUpgrade.Settings[enabledRegions.Count + 3].Value = greenMin;
                RegionMapUpgrade.Settings[enabledRegions.Count + 4].SavedValue = yellowMin;
                RegionMapUpgrade.Settings[enabledRegions.Count + 4].Value = yellowMin;
            }
            if (currentScene == "Gym")
            {
                if (touchscreen && !pastTouchscreen)
                {
                    prevStepDurations = new float[6] { playerRef.revolvingNumbers[0].stepDuration, playerRef.revolvingNumbers[1].stepDuration, playerRef.revolvingNumbers[2].stepDuration, pingRef.revolvingNumbers[0].stepDuration, pingRef.revolvingNumbers[1].stepDuration, pingRef.revolvingNumbers[2].stepDuration };
                    playerRef.revolvingNumbers[0].stepDuration = 0.01f;
                    playerRef.revolvingNumbers[1].stepDuration = 0.01f;
                    playerRef.revolvingNumbers[2].stepDuration = 0.01f;
                    pingRef.revolvingNumbers[0].stepDuration = 0.01f;
                    pingRef.revolvingNumbers[1].stepDuration = 0.01f;
                    pingRef.revolvingNumbers[2].stepDuration = 0.01f;
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(false);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.zero;
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition = new Vector3(Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.x, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.y - 1, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.z);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(false);
                    foreach (Region region in Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions)
                    {
                        region.gameObject.layer = 22;
                    }
                }
                else if (!touchscreen && pastTouchscreen)
                {
                    playerRef.revolvingNumbers[0].stepDuration = prevStepDurations[0];
                    playerRef.revolvingNumbers[1].stepDuration = prevStepDurations[1];
                    playerRef.revolvingNumbers[2].stepDuration = prevStepDurations[2];
                    pingRef.revolvingNumbers[0].stepDuration = prevStepDurations[3];
                    pingRef.revolvingNumbers[1].stepDuration = prevStepDurations[4];
                    pingRef.revolvingNumbers[2].stepDuration = prevStepDurations[5];
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(true);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.one;
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition = new Vector3(Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.x, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.y + 1, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.z);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(true);
                    foreach (Region region in Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions)
                    {
                        region.gameObject.layer = 0;
                    }
                }
                if ((mapColor != pastMapColor) || (mapColorGrey != pastMapColorGrey) || (greenMin != pastGreenMin) || (yellowMin != pastYellowMin))
                {
                    foreach(Region region in regions)
                    {
                        if (mapColor)
                        {
                            int playerCount = int.Parse(region.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text);
                            if (!mapColorGrey && playerCount == -1)
                            {
                                region.glowGameObject.GetComponent<MeshRenderer>().material.shader = regionGlow;
                                region.UnHighlight();
                            }
                            else
                            {
                                Color color;
                                if (playerCount >= greenMin) { color = new Color(0f, 0.5f, 0f); } //green
                                else if (playerCount < yellowMin) { color = new Color(0.5f, 0f, 0f); } //red
                                else { color = new Color(0.71f, 0.65f, 0f); } //yellow
                                if (mapColorGrey && playerCount == -1) { color = Color.gray; }//grey if unchecked
                                region.glowGameObject.GetComponent<MeshRenderer>().material.shader = urp;
                                region.glowGameObject.GetComponent<MeshRenderer>().material.color = color;
                                region.Highlight();
                            }
                        }
                        else
                        {
                            region.glowGameObject.GetComponent<MeshRenderer>().material.shader = regionGlow;
                            if (region.regionCode != NetworkManager.instance.networkConfig.FixedRegion)
                            {
                                region.UnHighlight();
                            }
                        }
                    }
                }
                if (showPing != pastShowPing)
                {
                    if (showPing)
                    {
                        Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(2).GetChild(4).gameObject.SetActive(true);
                    }
                    else
                    {
                        Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(2).GetChild(4).gameObject.SetActive(false);
                    }
                }
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
            bail = true;
        }

        private void mapInit()
        {
            bail = false;
            if ((currentScene == "Gym") && !bail)
            {
                regionSelector = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).GetChild(0).GetComponent<RegionSelector>();
                regions = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions;
                MelonCoroutines.Start(GetPlayerCounts());
            }
            else
            {
                if (bail) { bail = false; }
                else { requeueing = false; }
            }
        }

        private IEnumerator GetPlayerCounts()
        {
            running = true;
            DateTime startTime = DateTime.Now;
            float waitTime = 2f;
            string originalRegion = NetworkManager.instance.networkConfig.FixedRegion;
            float prevUpdateInterval = regionSelector.updateInterval;
            playerRef = regionSelector.playerNumberCollection;
            pingRef = regionSelector.pingNumberCollection;
            prevStepDurations = new float[6] { playerRef.revolvingNumbers[0].stepDuration, playerRef.revolvingNumbers[1].stepDuration, playerRef.revolvingNumbers[2].stepDuration, pingRef.revolvingNumbers[0].stepDuration, pingRef.revolvingNumbers[1].stepDuration, pingRef.revolvingNumbers[2].stepDuration };
            regionCountText.Clear();
            regionSelector.updateInterval = 0.1f;
            playerRef.revolvingNumbers[0].stepDuration = 0.01f;
            playerRef.revolvingNumbers[1].stepDuration = 0.01f;
            playerRef.revolvingNumbers[2].stepDuration = 0.01f;
            pingRef.revolvingNumbers[0].stepDuration = 0.01f;
            pingRef.revolvingNumbers[1].stepDuration = 0.01f;
            pingRef.revolvingNumbers[2].stepDuration = 0.01f;
            while (Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(7).GetChild(1).GetComponent<TextMeshProUGUI>().text != regionTitles[regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion)])
            {
                yield return new WaitForFixedUpdate();
            }
            float playerCount = 0;
            while (playerCount == 0)
            {
                yield return new WaitForFixedUpdate();
                while (regionSelector.playerNumberCollection.revolvingNumbers[0].rotateCoroutine != null || playerRef.revolvingNumbers[1].rotateCoroutine != null || playerRef.revolvingNumbers[2].rotateCoroutine != null)
                {
                    yield return new WaitForFixedUpdate();
                }
                playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + playerRef.revolvingNumbers[2].currentNumber;
            }
            float lastPlayerCount = playerCount;
            bool[] tempEnabledRegions = new bool[enabledRegions.Count];
            enabledRegions.CopyTo(tempEnabledRegions, 0);
            if (touchscreen)
            {
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(false);
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.zero;
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition = new Vector3(Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.x, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.y - 1, Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localPosition.z);
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(false);
            }
            for (int i = 0; i < regionOrder.Count; i++)
            {
                if (bail) { bail = false; yield break; }
                Region region = regions[regionOrder[i]];
                region.gameObject.AddComponent<HandColliderMapCheck>();
                if (touchscreen)
                {
                    region.gameObject.layer = 22;
                }
                if (tempEnabledRegions[i] && !requeueing)
                {
                    region.Select();
                    yield return new WaitForFixedUpdate();
                    while (Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(7).GetChild(1).GetComponent<TextMeshProUGUI>().text != regionTitles[regionCodes.IndexOf(region.RegionCode)])
                    {
                        yield return new WaitForFixedUpdate();
                    }
                    yield return new WaitForSeconds(0.25f);
                    playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + playerRef.revolvingNumbers[2].currentNumber;
                    lastPlayerCount = playerCount;
                    DateTime endCheck = DateTime.Now.AddSeconds(waitTime);
                    if (waitTime != 1f) { waitTime = 1f; }
                    while (DateTime.Now <= endCheck && playerCount == lastPlayerCount)
                    {
                        yield return new WaitForFixedUpdate();
                        while (regionSelector.playerNumberCollection.revolvingNumbers[0].rotateCoroutine != null || playerRef.revolvingNumbers[1].rotateCoroutine != null || playerRef.revolvingNumbers[2].rotateCoroutine != null)
                        {
                            yield return new WaitForFixedUpdate();
                        }
                        playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + playerRef.revolvingNumbers[2].currentNumber;
                    }
                    lastPlayerCount = playerCount;
                }
                else
                {
                    playerCount = 0;
                }
                if (mapColor)
                {
                    if (!mapColorGrey && playerCount == 0)
                    {
                        region.glowGameObject.GetComponent<MeshRenderer>().material.shader = regionGlow;
                    }
                    else
                    {
                        Color color;
                        if (playerCount >= greenMin + 1) { color = new Color(0f, 0.5f, 0f); } //green
                        else if (playerCount < yellowMin + 1) { color = new Color(0.5f, 0f, 0f); } //red
                        else { color = new Color(0.71f, 0.65f, 0f); } //yellow
                        if (mapColorGrey && playerCount == 0) { color = Color.gray; }//grey if unchecked
                        if (!mapColorGrey && playerCount == 0)
                        {
                            region.glowGameObject.GetComponent<MeshRenderer>().material.shader = regionGlow;
                        }
                        else
                        {
                            region.glowGameObject.GetComponent<MeshRenderer>().material.shader = urp;
                            region.glowGameObject.GetComponent<MeshRenderer>().material.color = color;
                            region.Highlight();
                        }
                    }
                }
                GameObject regionText = Calls.Create.NewText((playerCount - 1).ToString(), 0.5f, Color.black, Vector3.zero, Quaternion.identity);
                regionText.name = region.gameObject.name;
                regionText.transform.parent = region.transform;
                regionText.transform.localPosition = new Vector3(0, 0.05f, 0);
                regionText.transform.localRotation = Quaternion.Euler(90, 0, 0);
                regionText.GetComponent<TextMeshPro>().alignment = TextAlignmentOptions.Center;
                regionText.GetComponent<TextMeshPro>().enableWordWrapping = false;
                if ((playerCount <= 1) || (!tempEnabledRegions[i]) || requeueing) { regionText.SetActive(false); }
                regionCountText.Add(regionText);
            }
            if (!requeueing) { regions[regionCodes.IndexOf(originalRegion)].Select(); }
            regionSelector.updateInterval = prevUpdateInterval;
            if (!touchscreen)
            {
                playerRef.revolvingNumbers[0].stepDuration = prevStepDurations[0];
                playerRef.revolvingNumbers[1].stepDuration = prevStepDurations[1];
                playerRef.revolvingNumbers[2].stepDuration = prevStepDurations[2];
                pingRef.revolvingNumbers[0].stepDuration = prevStepDurations[3];
                pingRef.revolvingNumbers[1].stepDuration = prevStepDurations[4];
                pingRef.revolvingNumbers[2].stepDuration = prevStepDurations[5];
            }
            Transform temp = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetChild(0).GetChild(11);
            temp.localPosition = new Vector3(temp.localPosition.x, temp.localPosition.y + 0.018f, temp.localPosition.z);
            regionCountText[0].transform.localPosition = new Vector3(0, 0.049f, 0);
            regions[11].glowGameObject.transform.localPosition = new Vector3(regions[11].glowGameObject.transform.localPosition.x, regions[11].glowGameObject.transform.localPosition.y + 0.017f, regions[11].glowGameObject.transform.localPosition.z);
            MelonCoroutines.Start(PlayerCountForActiveRegionWatcher());
            float time = (float)((DateTime.Now - startTime).TotalSeconds);
            GameObject footer = Calls.Create.NewText("Last Time To Load: " + time.ToString("0.##") + " Seconds", 0.25f, new Color(1f, 0.77f, 0.5f), Vector3.zero, Quaternion.identity);
            footer.name = "TimeToLoad";
            footer.transform.parent = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(2).transform;
            footer.transform.localPosition = new Vector3(-0.3f, 0.07f, -0.27f);
            footer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            running = false;
            yield break;
        }

        private IEnumerator PlayerCountForActiveRegionWatcher()
        {
            yield return new WaitForSeconds(1);
            bool init = false;
            int lastPlayerCount = int.Parse(regions[regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion)].gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text);
            int playerCount = 0;
            int lastPingCount = 0;
            int pingCount = 0;
            int currentRegionSpot = -1;
            Region region = null;
            TextMeshPro regionTMP = null;
            GameObject pingCountGO = Calls.Create.NewText("Ping: -1", 0.5f, Color.black, Vector3.zero, Quaternion.identity);
            TextMeshPro pingTMP = pingCountGO.GetComponent<TextMeshPro>();
            pingCountGO.name = "Ping";
            pingCountGO.transform.parent = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(2);
            pingCountGO.transform.localPosition = new Vector3(-0.127f, 0.148f, 0.385f);
            pingCountGO.transform.localRotation = Quaternion.Euler(90, 0, 0);
            pingTMP.enableWordWrapping = false;
            if (!showPing)
            {
                yield return new WaitForFixedUpdate();
                pingCountGO.SetActive(false);
            }
            while ((currentScene == "Gym") && !bail)
            {
                playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + playerRef.revolvingNumbers[2].currentNumber;
                pingCount = (regionSelector.pingNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.pingNumberCollection.revolvingNumbers[1].currentNumber * 10) + pingRef.revolvingNumbers[2].currentNumber;
                if (!init || (playerCount != lastPlayerCount) || (currentRegionSpot != regionOrder.IndexOf(regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion))))
                {
                    if (currentRegionSpot != regionOrder.IndexOf(regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion)))
                    {
                        currentRegionSpot = regionOrder.IndexOf(regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion));
                        region = regions[regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion)];
                        regionTMP = regionCountText[currentRegionSpot].GetComponent<TextMeshPro>();
                    }
                    if (mapColor && (region.glowGameObject.GetComponent<MeshRenderer>().material.shader != urp))
                    {
                        region.glowGameObject.GetComponent<MeshRenderer>().material.shader = urp;
                    }
                    Color color;
                    if (playerCount >= greenMin + 1) { color = new Color(0f, 0.5f, 0f); } //green
                    else if (playerCount < yellowMin + 1) { color = new Color(0.5f, 0f, 0f); } //red
                    else { color = new Color(0.71f, 0.65f, 0f); } //yellow
                    region.glowGameObject.GetComponent<MeshRenderer>().material.color = color;
                    regionTMP.text = (playerCount - 1).ToString();
                    if (playerCount <= 1)
                    {
                        regionCountText[currentRegionSpot].SetActive(false);
                    }
                    else if (!regionCountText[currentRegionSpot].active)
                    {
                        regionCountText[currentRegionSpot].SetActive(true);
                    }
                    lastPlayerCount = playerCount;
                    init = true;
                }
                if (showPing && (pingCount != lastPingCount))
                {
                    pingTMP.text = $"Ping: {pingCount}";
                    lastPingCount = pingCount;
                }
                yield return new WaitForFixedUpdate();
            }
            if (bail) { bail = false; }
            yield break;
        }

        private void AddButtonListeners()
        {
            if (Calls.Players.IsHost())
            {
                if (currentScene == "Map0")
                {
                    Calls.GameObjects.Map0.Logic.MatchSlabOne.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        requeueing = true;
                    }));
                }
                else if (currentScene == "Map1")
                {
                    Calls.GameObjects.Map1.Logic.MatchSlabOne.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        requeueing = true;
                    }));
                }
            }
            else
            {
                if (currentScene == "Map0")
                {
                    Calls.GameObjects.Map0.Logic.MatchSlabTwo.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        requeueing = true;
                    }));
                }
                else if (currentScene == "Map1")
                {
                    Calls.GameObjects.Map1.Logic.MatchSlabTwo.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        requeueing = true;
                    }));
                }
            }
        }

        [HarmonyPatch(typeof(Region), "UnHighlight")]
        public static class MatchmakingType
        {
            private static void Postfix(ref Region __instance)
            {
                try
                {
                    if ((Main.mapColor && Main.mapColorGrey) || (Main.mapColor && (__instance.gameObject.transform.GetChild(0).GetComponent<TextMeshPro>().text != "-1")))
                    {
                        __instance.Highlight();
                    }
                }
                catch { }
            }
        }
    }
}
