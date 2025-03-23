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

namespace RegionMapUpgrade
{
    public static class BuildInfo
    {
        public const string ModName = "RegionMapUpgrade";
        public const string ModVersion = "1.0.2";
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
            this.gameObject.GetComponent<Region>().Select();
        }
    }

    public class Main : MelonMod
    {
        private string currentScene = "Loader";
        private Mod RegionMapUpgrade = new Mod();
        private bool[] enabledRegions = new bool[13] { false, false, false, false, false, false, false, false, false, false, false, false, false};
        private Shader urp;
        public static bool running = false;
        public static bool touchscreen = false;
        private bool rematching = false;
        public static bool touchingARegion = false;

        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        public override void OnLateInitializeMelon()
        {
            //setup ModUI Mod
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
            RegionMapUpgrade.AddToList("Russia", true, 0, "Toggle's Russia Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("Russia, East", true, 0, "Toggle's Russia, East Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South Africa", true, 0, "Toggle's South Africa Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South America", true, 0, "Toggle's South America Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("South Korea", true, 0, "Toggle's South Korea Check On/Off", new Tags { });
            RegionMapUpgrade.AddToList("TouchScreen", true, 0, "Toggle's TouchScreen Version of RegionBoard On/Off", new Tags { });
            RegionMapUpgrade.GetFromFile();
            //Call Save to set variables to start
            Save();
            Calls.onMapInitialized += mapInit;
            Calls.onMatchStarted += AddButtonListeners;
            UI.instance.UI_Initialized += UIInit;
            RegionMapUpgrade.ModSaved += Save;
            urp = Shader.Find("Universal Render Pipeline/Lit");
        }

        private void UIInit()
        {
            UI.instance.AddMod(RegionMapUpgrade);
        }

        private void Save()
        {
            //for each region, store the value of on/off into the list
            for (int i = 0; i < enabledRegions.Length; i++)
            {
                enabledRegions[i] = (bool)RegionMapUpgrade.Settings[i].SavedValue;
            }
            //stores touchscreen variable
            touchscreen = (bool)RegionMapUpgrade.Settings[enabledRegions.Length].SavedValue;
            //set touchscreen or return Handle
            if (currentScene == "Gym")
            {
                if (touchscreen)
                {
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(false);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.zero;
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(false);
                    foreach (Region region in Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions)
                    {
                        region.gameObject.layer = 22;
                    }
                }
                else
                {
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(true);
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.one;
                    Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(true);
                    foreach (Region region in Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions)
                    {
                        region.gameObject.layer = 0;
                    }
                }
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            currentScene = sceneName;
        }

        private void mapInit()
        {
            if (!rematching && (currentScene == "Gym"))
            {
                MelonCoroutines.Start(GetPlayerCounts());
            }
            else
            {
                rematching = false;
            }
        }

        private IEnumerator GetPlayerCounts()
        {
            //variables
            running = true;
            DateTime startTime = DateTime.Now;
            float waitTime = 1.5f;
            string originalRegion = NetworkManager.instance.networkConfig.FixedRegion;
            RegionSelector regionSelector = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).GetChild(0).GetComponent<RegionSelector>();
            Il2CppReferenceArray<Region> regions = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(3).GetComponent<WorldMap>().regions;
            float prevUpdateInterval = regionSelector.updateInterval;
            float[] prevStepDurations = new float[6] { regionSelector.playerNumberCollection.revolvingNumbers[0].stepDuration, regionSelector.playerNumberCollection.revolvingNumbers[1].stepDuration, regionSelector.playerNumberCollection.revolvingNumbers[2].stepDuration, regionSelector.pingNumberCollection.revolvingNumbers[0].stepDuration, regionSelector.pingNumberCollection.revolvingNumbers[1].stepDuration, regionSelector.pingNumberCollection.revolvingNumbers[2].stepDuration };
            List<string> regionCodes = new List<string>() { "asia", "au", "cae", "eu", "in", "jp", "ru", "rue", "za", "sa", "kr", "us", "usw" };
            string[] regionTitles = new string[13] { "Asia", "Australia", "Canada", "Europe", "India", "Japan", "Russia", "Russia, East", "South Africa", "South America", "South Korea", "USA, East", "USA, West" };
            int[] regionOrder = new int[13] { 11, 12, 3, 1, 2, 0, 4, 5, 6, 7, 8, 9, 10 };
            //speed up region selector
            regionSelector.updateInterval = 0.1f;
            regionSelector.playerNumberCollection.revolvingNumbers[0].stepDuration = 0.01f;
            regionSelector.playerNumberCollection.revolvingNumbers[1].stepDuration = 0.01f;
            regionSelector.playerNumberCollection.revolvingNumbers[2].stepDuration = 0.01f;
            regionSelector.pingNumberCollection.revolvingNumbers[0].stepDuration = 0.01f;
            regionSelector.pingNumberCollection.revolvingNumbers[1].stepDuration = 0.01f;
            regionSelector.pingNumberCollection.revolvingNumbers[2].stepDuration = 0.01f;
            //while connecting to region
            while (Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(7).GetChild(1).GetComponent<TextMeshProUGUI>().text != regionTitles[regionCodes.IndexOf(NetworkManager.instance.networkConfig.FixedRegion)])
            {
                yield return new WaitForFixedUpdate();
            }
            //grab player count
            float playerCount = 0;
            while (playerCount == 0)
            {
                yield return new WaitForFixedUpdate();
                //while wheels are rotating, wait
                while (regionSelector.playerNumberCollection.revolvingNumbers[0].rotateCoroutine != null || regionSelector.playerNumberCollection.revolvingNumbers[1].rotateCoroutine != null || regionSelector.playerNumberCollection.revolvingNumbers[2].rotateCoroutine != null)
                {
                    yield return new WaitForFixedUpdate();
                }
                //sets each dial to know the player count
                playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + regionSelector.playerNumberCollection.revolvingNumbers[2].currentNumber;
            }
            //store new last playercount
            float lastPlayerCount = playerCount;
            //create array of toggles to not be effected by a save while it's running
            bool[] tempEnabledRegions = new bool[enabledRegions.Length];
            enabledRegions.CopyTo(tempEnabledRegions, 0);
            if (touchscreen)
            {
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(0).gameObject.SetActive(false);
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(1).localScale = Vector3.zero;
                Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(5).gameObject.SetActive(false);
            }
            //loop for each region
            for (int i = 0; i < regionOrder.Length; i++)
            {
                Region region = regions[regionOrder[i]];
                //Add Hand Collider Component to Region
                region.gameObject.AddComponent<HandColliderMapCheck>();
                if (touchscreen)
                {
                    region.gameObject.layer = 22;
                }
                //continue if region is turned off
                if (!tempEnabledRegions[i]) { continue; }
                //select region
                region.Select();
                yield return new WaitForFixedUpdate();
                //while region text is not the text it should be, wait
                while (Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(7).GetChild(1).GetComponent<TextMeshProUGUI>().text != regionTitles[regionCodes.IndexOf(region.RegionCode)])
                {
                    yield return new WaitForFixedUpdate();
                }
                //sets each dial to know the player count
                playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + regionSelector.playerNumberCollection.revolvingNumbers[2].currentNumber;
                //sets the max time till it continues
                DateTime endCheck = DateTime.Now.AddSeconds(waitTime);
                //this is for 1.5 seconds wait time on first region adds stability to first reading but not needed on others
                if (waitTime != 1f) { waitTime = 1f; }
                //while wait time not elapsed or same player count as last player count
                while (DateTime.Now <= endCheck && playerCount == lastPlayerCount)
                {
                    yield return new WaitForFixedUpdate();
                    //while wheels are rotating, wait
                    while (regionSelector.playerNumberCollection.revolvingNumbers[0].rotateCoroutine != null || regionSelector.playerNumberCollection.revolvingNumbers[1].rotateCoroutine != null || regionSelector.playerNumberCollection.revolvingNumbers[2].rotateCoroutine != null)
                    {
                        yield return new WaitForFixedUpdate();
                    }
                    //sets each dial to know the player count
                    playerCount = (regionSelector.playerNumberCollection.revolvingNumbers[0].currentNumber * 100) + (regionSelector.playerNumberCollection.revolvingNumbers[1].currentNumber * 10) + regionSelector.playerNumberCollection.revolvingNumbers[2].currentNumber;
                }
                //set new last playercount
                lastPlayerCount = playerCount;
                //find which color
                Color color;
                if (playerCount <= 1) { color = new Color(0.5f, 0f, 0f); } //red
                else if (playerCount >= 10) { color = new Color(0f, 0.5f, 0f); } //green
                else { color = new Color(0.71f, 0.65f, 0f); } //yellow
                //set shader so I can set color
                region.glowGameObject.GetComponent<MeshRenderer>().material.shader = urp;
                //set color
                region.glowGameObject.GetComponent<MeshRenderer>().material.color = color;
                //skips if 0 players in region
                if (playerCount == 1) { continue; }
                //create text and move it
                GameObject regionText = Calls.Create.NewText((playerCount - 1).ToString(), 0.5f, Color.black, Vector3.zero, Quaternion.identity);
                regionText.name = region.gameObject.name + "Text";
                regionText.transform.parent = region.transform;
                regionText.transform.localPosition = new Vector3(0, 0.05f, 0);
                regionText.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            //select original region
            regions[regionCodes.IndexOf(originalRegion)].Select();
            //return region selector to normal speeds
            regionSelector.updateInterval = prevUpdateInterval;
            regionSelector.playerNumberCollection.revolvingNumbers[0].stepDuration = prevStepDurations[0];
            regionSelector.playerNumberCollection.revolvingNumbers[1].stepDuration = prevStepDurations[1];
            regionSelector.playerNumberCollection.revolvingNumbers[2].stepDuration = prevStepDurations[2];
            regionSelector.pingNumberCollection.revolvingNumbers[0].stepDuration = prevStepDurations[3];
            regionSelector.pingNumberCollection.revolvingNumbers[1].stepDuration = prevStepDurations[4];
            regionSelector.pingNumberCollection.revolvingNumbers[2].stepDuration = prevStepDurations[5];
            float time = (float)((DateTime.Now - startTime).TotalSeconds);
            int timemultiplied = (int)(time * 100);
            time = (float)timemultiplied / 100;
            //create Text for Time To Load
            GameObject footer = Calls.Create.NewText("Last Time To Load: " + time + " Seconds", 0.25f, new Color(1f, 0.77f, 0.5f), Vector3.zero, Quaternion.identity);
            footer.name = "TimeToLoad";
            footer.transform.parent = Calls.GameObjects.Gym.Logic.HeinhouserProducts.RegionSelector.Model.GetGameObject().transform.GetChild(2).transform;
            footer.transform.localPosition = new Vector3(-0.3f, 0.06f, -0.27f);
            footer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            running = false;
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
                        rematching = true;
                    }));
                }
                else
                {
                    Calls.GameObjects.Map1.Logic.MatchSlabOne.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        rematching = true;
                    }));
                }
            }
            else
            {
                if (currentScene == "Map0")
                {
                    Calls.GameObjects.Map0.Logic.MatchSlabTwo.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        rematching = true;
                    }));
                }
                else
                {
                    Calls.GameObjects.Map1.Logic.MatchSlabTwo.MatchSlab.SlabBuddyMatchVariant.MatchForm.ReQueueButton.InteractionButton.Button.GetGameObject().GetComponent<InteractionButton>().OnPressed.AddListener(new System.Action(() =>
                    {
                        rematching = true;
                    }));
                }
            }
        }

        [HarmonyPatch(typeof(Region), "UnHighlight")]
        public static class MatchmakingType
        {
            private static void Postfix(ref Region __instance)
            {
                //rehighlight when unhighlighted
                __instance.Highlight();
            }
        }
    }
}
