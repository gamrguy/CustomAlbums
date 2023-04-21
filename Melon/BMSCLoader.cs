using Assets.Scripts.GameCore;
using Assets.Scripts.GameCore.Managers;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using FormulaBase;
using GameLogic;
using Il2Generic = Il2CppSystem.Collections.Generic;
using Il2Newtonsoft = Il2CppNewtonsoft.Json.Linq;
using Newtonsoft.Json;
using PeroTools2.Resources;
using Spine;
using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json.Linq;
using PeroPeroGames.GlobalDefines;
using Melon.Misc;
using System.Linq;

namespace CustomAlbums
{
	public static class BMSCLoader
	{
        private static Logger Log = new Logger(nameof(BMSCLoader));

        enum BossState
        {
            OFFSCREEN,
            IDLE,
            PHASE_1,
            PHASE_2
        }

        enum AnimAlignment
        {
            LEFT = -1,
            RIGHT = 1
        }

        /// <summary>
        /// BMS class copied from vanilla game
        /// </summary>
        public class BMS
        {
            [Flags]
            public enum ChannelType
            {
                /// <summary>
                /// Channel does not support anything.
                /// </summary>
                NONE = 0,
                /// <summary>
                /// Channel supports the Ground Lane.
                /// </summary>
                GROUND = 1,
                /// <summary>
                /// Channel supports the Air Lane.
                /// </summary>
                AIR = 2,
                /// <summary>
                /// Channel supports standard events.
                /// </summary>
                EVENT = 4,
                /// <summary>
                /// Channel supports scene events.
                /// </summary>
                SCENE = 8,
                
                /// <summary>
                /// Notes in this channel become Heart Enemies if possible.
                /// </summary>
                SP_BLOOD = 16,
                /// <summary>
                /// Notes in this channel become Tap Holds if possible.
                /// </summary>
                SP_TAP_HOLDS = 32,
                /// <summary>
                /// This channel is used for BPM changes, with the value directly present.
                /// </summary>
                SP_BPM_DIRECT = 64,
                /// <summary>
                /// This channel is used for BPM changes, with the value placed in a lookup table.
                /// </summary>
                SP_BPM_LOOKUP = 128,
                /// <summary>
                /// This channel is used for time signature changes.
                /// </summary>
                SP_TIMESIG = 256,
                /// <summary>
                /// This channel is used for scroll speed changes.
                /// </summary>
                SP_SCROLL = 512
            }

            public static readonly Dictionary<string, ChannelType> channels = new() {
                ["01"] = ChannelType.SCENE,
                ["02"] = ChannelType.SP_TIMESIG,
                ["03"] = ChannelType.SP_BPM_DIRECT,
                ["08"] = ChannelType.SP_BPM_LOOKUP,
                ["15"] = ChannelType.EVENT,
                ["16"] = ChannelType.EVENT,
                ["18"] = ChannelType.EVENT,
                ["11"] = ChannelType.AIR | ChannelType.EVENT,
                ["13"] = ChannelType.AIR | ChannelType.EVENT,
                ["31"] = ChannelType.AIR | ChannelType.EVENT | ChannelType.SP_BLOOD,
                ["33"] = ChannelType.AIR | ChannelType.EVENT | ChannelType.SP_BLOOD,
                ["51"] = ChannelType.AIR | ChannelType.EVENT,
                ["53"] = ChannelType.AIR | ChannelType.EVENT,
                ["D1"] = ChannelType.AIR | ChannelType.EVENT | ChannelType.SP_TAP_HOLDS,
                ["D3"] = ChannelType.AIR | ChannelType.EVENT | ChannelType.SP_TAP_HOLDS,
                ["12"] = ChannelType.GROUND | ChannelType.EVENT,
                ["14"] = ChannelType.GROUND | ChannelType.EVENT,
                ["32"] = ChannelType.GROUND | ChannelType.EVENT | ChannelType.SP_BLOOD,
                ["34"] = ChannelType.GROUND | ChannelType.EVENT | ChannelType.SP_BLOOD,
                ["52"] = ChannelType.GROUND | ChannelType.EVENT,
                ["54"] = ChannelType.GROUND | ChannelType.EVENT,
                ["D2"] = ChannelType.GROUND | ChannelType.EVENT | ChannelType.SP_TAP_HOLDS,
                ["D4"] = ChannelType.GROUND | ChannelType.EVENT | ChannelType.SP_TAP_HOLDS,
                ["SC"] = ChannelType.SP_SCROLL
            };

            public static Dictionary<string, NoteConfigData> noteDataMap;

            public JObject info;
            public JArray notes;
            public JArray notesPercent;
            public string md5;

            public float GetBpm() {
                JToken jToken = info["BPM"];
                if(IsNullOrEmpty(jToken)) {
                    return (float)info["BPM01"];
                }
                return (float)jToken;
            }

            private static bool IsNullOrEmpty(JToken token) {
                if(token != null && (token.Type != JTokenType.Array || token.HasValues) && (token.Type != JTokenType.Object || token.HasValues) && (token.Type != JTokenType.String || !(token.ToString() == string.Empty))) {
                    return token.Type == JTokenType.Null;
                }
                return true;
            }

            private static void InitNoteDatas() {
                noteDataMap = new Dictionary<string, NoteConfigData>();

                // TODO: Find a different way to cache all this nonsense
                foreach(var config in NoteDataMananger.instance.noteDatas) {
                    // Ignore april fool's variants (these are handled elsewhere)
                    if(config.prefab_name.EndsWith("_fool")) continue;
                    // Ignore phase 2 boss gears
                    if(config.type == (int)NoteType.Block && config.boss_action.EndsWith("_atk_2")) continue;

                    // Scene setting of "0" is a wildcard
                    var anyScene = config.scene == "0";
                    // Notes with these values are extremely likely to be events, and get registered to all pathways
                    var anyPathway = config.pathway == 0 
                        && config.score == 0 
                        && config.fever == 0 
                        && config.damage == 0;
                    // Boss type, None type, boss mashers, and events get registered to all speeds
                    var anySpeed = config.type == (int)NoteType.Boss
                        || config.type == (int)NoteType.None
                        || config.ibms_id == "16"
                        || config.ibms_id == "17";

                    var speeds = new List<int> { config.speed };
                    var scenes = new List<string> { config.scene };
                    var pathways = new List<int> { config.pathway };

                    // Use all speeds if any speed
                    if(anySpeed) {
                        speeds = new List<int> { 1, 2, 3 };
                    }
                    // Use all scenes if any scene
                    if(anyScene) {
                        scenes = new List<string>();
                        foreach(var scene in Singleton<StageBattleComponent>.instance.sceneInfo) {
                            // Special handling for collectibles in touhou scene
                            if(config.type == (int)NoteType.Hp || config.type == (int)NoteType.Music) {
                                if(scene.Value == 8) {
                                    continue;
                                }
                            }
                            scenes.Add($"scene_0{scene.Value}");
                        }
                    }
                    // Use all pathways if any pathway
                    if(anyPathway) {
                        pathways = new List<int> { 0, 1 };
                    }

                    foreach(var pathway in pathways) {
                        foreach(var speed in speeds) {
                            foreach(var scene in scenes) {
                                var key = GetNoteDataKey(config.ibms_id, pathway, speed, scene);
                                if(!noteDataMap.ContainsKey(key)) {
                                    noteDataMap[key] = config;
                                } else {
                                    Log.Warning($"Duplicate NoteConfigData detected: {key}");
                                }
                            }
                        }
                    }
                }
            }

            private static string GetNoteDataKey(string ibmsId, int pathway, int speed, string scene) {
                return $"{ibmsId}-{pathway}-{speed}-{scene}";
            }

            private static NoteConfigData TryFindNoteData(string key) {
                if(noteDataMap.TryGetValue(key, out var config)) {
                    return config;
                }

                return new NoteConfigData();
            }

            public static ChannelType GetChannelType(string channel) {
                return channels.ContainsKey(channel) ? channels[channel] : ChannelType.NONE;
            }

            public Il2Generic.List<SceneEvent> GetSceneEvents() {
                var list = new Il2Generic.List<SceneEvent>();
                for(int i = 0; i < notes.Count; i++) {
                    var note = notes[i];
                    var text = (string)note["value"];
                    if(!string.IsNullOrEmpty(text)) {
                        var channel = (string)note["tone"];
                        if(GetChannelType(channel).HasFlag(ChannelType.SCENE)) {
                            list.Add(new SceneEvent() {
                                time = (Il2CppSystem.Decimal)(float)note["time"],
                                uid = $"SceneEvent/{text}"
                            });
                        } else if(GetChannelType(channel).HasFlag(ChannelType.SP_BPM_DIRECT) || GetChannelType(channel).HasFlag(ChannelType.SP_BPM_LOOKUP)) {
                            list.Add(new SceneEvent() {
                                time = (Il2CppSystem.Decimal)(float)note["time"],
                                uid = $"SceneEvent/OnBPMChanged",
                                value = (string)note["value"]
                            });
                        }
                    }
                }

                return list;
            }

            public JArray GetNoteDatas() {
                if(noteDataMap == null) {
                    InitNoteDatas();
                }

                var processed = new JArray();
                var scene = (string)info["GENRE"];
                var speedAir = 1;
                var speedGround = 1;
                if(info.TryGetValue("PLAYER", out var baseSpeed)) {
                    speedGround = speedAir = int.Parse((string)baseSpeed);
                }
                var objId = 1;
                for(int i = 0; i < notes.Count; i++) {
                    var note = notes[i];
                    var ibmsId = (string)note["value"];
                    if(string.IsNullOrEmpty(ibmsId)) {
                        continue;
                    }
                    var channel = (string)note["tone"];
                    var channelType = GetChannelType(channel);
                    var pathway = -1;

                    // Lane type
                    if(channelType.HasFlag(ChannelType.AIR)) {
                        pathway = 1;
                    } else if(channelType.HasFlag(ChannelType.GROUND) || channelType.HasFlag(ChannelType.EVENT)) {
                        pathway = 0;
                    }

                    // Any unrecognized or non-standard channel is ignored
                    if(pathway == -1) {
                        continue;
                    }

                    // Speed changes
                    if(ibmsId == "0O") {
                        speedGround = speedAir = 1;
                    } else if(ibmsId == "0P") {
                        speedGround = speedAir = 2;
                    } else if(ibmsId == "0Q") {
                        speedGround = speedAir = 3;
                    } else if(ibmsId == "0R") {
                        speedGround = 1;
                    } else if(ibmsId == "0S") {
                        speedGround = 2;
                    } else if(ibmsId == "0T") {
                        speedGround = 3;
                    } else if(ibmsId == "0U") {
                        speedAir = 1;
                    } else if(ibmsId == "0V") {
                        speedAir = 2;
                    } else if(ibmsId == "0W") {
                        speedAir = 3;
                    }

                    var time = (decimal)note["time"];
                    var holdLength = 0m;
                    var speed = (pathway == 1) ? speedAir : speedGround;

                    // Find note config
                    // The matched NoteConfigData should have:
                    // - The same iBMS ID
                    // - The same scene (if it matters)
                    // - The same lane (if it matters)
                    // - The same speed (if it matters)
                    var noteConfigData = TryFindNoteData(GetNoteDataKey(ibmsId, pathway, speed, scene));
                    if(string.IsNullOrEmpty(noteConfigData.uid)) {
                        continue;
                    }

                    // Hold Note / Masher Handling
                    var isHold = noteConfigData.type == (int)NoteType.Press || noteConfigData.type == (int)NoteType.Mul;
                    if(isHold) {
                        if(channelType.HasFlag(ChannelType.SP_TAP_HOLDS)) {
                            holdLength = 0.001m;
                        } else {
                            for(int k = i + 1; k < notes.Count; k++) {
                                var holdEndNote = notes[k];
                                var holdEndTime = (decimal)holdEndNote["time"];
                                var holdEndIbms = (string)holdEndNote["value"];
                                var holdEndCh = (string)holdEndNote["tone"];
                                if(holdEndIbms == ibmsId && holdEndCh == channel) {
                                    holdLength = holdEndTime - time;
                                    // This causes the next hold note to be skipped
                                    notes[k]["value"] = "";
                                    break;
                                }
                            }
                        }
                    }

                    processed.Add(new JObject {
                        ["id"] = objId++,
                        ["time"] = time,
                        ["note_uid"] = noteConfigData.uid,
                        ["length"] = holdLength,
                        ["pathway"] = pathway,
                        ["blood"] = !isHold && channelType.HasFlag(ChannelType.SP_BLOOD)
                    });
                }
                return processed;
            }
        }

        /// <summary>
        /// <br>Boss animation helper object.</br>
        /// <br>Boss state when viewed from the left (before)</br>
        /// <br>Boss should be in this state BEFORE performing animation</br>
        /// </summary>
        private static readonly Dictionary<string, BossState> animStatesLeft = new Dictionary<string, BossState> {
            { "in", BossState.OFFSCREEN },
            { "out", BossState.IDLE },
            { "boss_close_atk_1", BossState.IDLE },
            { "boss_close_atk_2", BossState.IDLE },
            { "multi_atk_48", BossState.IDLE },
            { "multi_atk_48_end", BossState.IDLE },
            { "boss_far_atk_1_L", BossState.PHASE_1 },
            { "boss_far_atk_1_R", BossState.PHASE_1 },
            { "boss_far_atk_2", BossState.PHASE_2 },
            { "boss_far_atk_1_start", BossState.IDLE },
            { "boss_far_atk_2_start", BossState.IDLE },
            { "boss_far_atk_1_end", BossState.PHASE_1 },
            { "boss_far_atk_2_end", BossState.PHASE_2 },
            { "atk_1_to_2", BossState.PHASE_1 },
            { "atk_2_to_1", BossState.PHASE_2 }
        };

        /// <summary>
        /// <br>Boss state when viewed from the right (after)</br>
        /// <br>Boss should be in this state AFTER performing animation</br>
        /// </summary>
        private static readonly Dictionary<string, BossState> animStatesRight = new Dictionary<string, BossState> {
            { "in", BossState.IDLE },
            { "out", BossState.OFFSCREEN },
            { "boss_close_atk_1", BossState.IDLE },
            { "boss_close_atk_2", BossState.IDLE },
            { "multi_atk_48", BossState.IDLE },
            { "multi_atk_48_end", BossState.OFFSCREEN },
            { "boss_far_atk_1_L", BossState.PHASE_1 },
            { "boss_far_atk_1_R", BossState.PHASE_1 },
            { "boss_far_atk_2", BossState.PHASE_2 },
            { "boss_far_atk_1_start", BossState.PHASE_1 },
            { "boss_far_atk_2_start", BossState.PHASE_2 },
            { "boss_far_atk_1_end", BossState.IDLE },
            { "boss_far_atk_2_end", BossState.IDLE },
            { "atk_1_to_2", BossState.PHASE_2 },
            { "atk_2_to_1", BossState.PHASE_1 }
        };

        /// <summary>
        /// <br>Which animation to insert when encountering a state discrepancy</br>
        /// <br>First key is the current state (right side), second key is the next state (left side)</br>
        /// <br>"0" is placed where an animation should not be inserted (the states match)</br>
        /// </summary>
        private static readonly Dictionary<BossState, Dictionary<BossState, string>> stateTransferAnims = new Dictionary<BossState, Dictionary<BossState, string>> {
            { BossState.OFFSCREEN, new Dictionary<BossState, string> {
                { BossState.OFFSCREEN, "0" },
                { BossState.IDLE, "in" },
                { BossState.PHASE_1, "boss_far_atk_1_start" },
                { BossState.PHASE_2, "boss_far_atk_2_start" }
            } },
            { BossState.IDLE, new Dictionary<BossState, string> {
                { BossState.OFFSCREEN, "out" },
                { BossState.IDLE, "0" },
                { BossState.PHASE_1, "boss_far_atk_1_start" },
                { BossState.PHASE_2, "boss_far_atk_2_start" }
            } },
            { BossState.PHASE_1, new Dictionary<BossState, string> {
                { BossState.OFFSCREEN, "out" },
                { BossState.IDLE, "boss_far_atk_1_end" },
                { BossState.PHASE_1, "0" },
                { BossState.PHASE_2, "atk_1_to_2" }
            } },
            { BossState.PHASE_2, new Dictionary<BossState, string> {
                { BossState.OFFSCREEN, "out" },
                { BossState.IDLE, "boss_far_atk_2_end" },
                { BossState.PHASE_1, "atk_2_to_1" },
                { BossState.PHASE_2, "0" }
            } }
        };

        /// <summary>
        /// <br>Which side the inserted transfer animation should align itself with</br>
        /// </summary>
        private static readonly Dictionary<string, AnimAlignment> transferAlignment = new Dictionary<string, AnimAlignment> {
            { "in", AnimAlignment.RIGHT },
            { "out", AnimAlignment.LEFT },
            { "boss_far_atk_1_start", AnimAlignment.RIGHT },
            { "boss_far_atk_2_start", AnimAlignment.RIGHT },
            { "boss_far_atk_1_end", AnimAlignment.LEFT },
            { "boss_far_atk_2_end", AnimAlignment.LEFT },
            { "atk_1_to_2", AnimAlignment.RIGHT },
            { "atk_2_to_1", AnimAlignment.RIGHT }
        };

        private static readonly List<MusicData> musicDatas = new List<MusicData>();
        private static Il2CppSystem.Decimal delay;
        private static Dictionary<string, NoteConfigData> noteDatasUid;
        private static Dictionary<string, Dictionary<string, NoteConfigData>> noteDatasBoss;

        /// <summary>
        /// A bms loader copied from MuseDash.
        /// 
        /// Ref: Assets.Scripts.GameCore.Managers.iBMSCManager
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="bmsName"></param>
        /// <returns></returns>
        public static BMS Load(Stream stream, string bmsName) {
			var bpmTones = new Dictionary<string, float>();
			var notesPercentDict = new Dictionary<int, JToken>();

			var info = new JObject();
			var notes = new JArray();

			var notesPercent = new JArray();
			var list = new List<JObject>();

			// Calculate MD5 of bms bytes
			string md5 = stream.ToArray().GetMD5().ToString("x2");
			stream.Position = 0;
			StreamReader streamReader = new StreamReader(stream);

			string txtLine;
			// Parse bms
			while((txtLine = streamReader.ReadLine()) != null) {
				txtLine = txtLine.Trim();
				if(!string.IsNullOrEmpty(txtLine) && txtLine.StartsWith("#")) {
					txtLine = txtLine.Remove(0, 1); // Remove left right space and start '#'

					if(txtLine.Contains(" ")) {
						// Parse info
						string infoKey = txtLine.Split(' ')[0];
						string infoValue = txtLine.Remove(0, infoKey.Length + 1);

						info[infoKey] = infoValue;
						if(infoKey == "BPM") {
							float freq = 60f / float.Parse(infoValue) * 4f;
							var jObject = new JObject();
							jObject["tick"] = 0f;
							jObject["freq"] = freq;
							list.Add(jObject);
						} else if(infoKey.Contains("BPM")) {
							bpmTones.Add(infoKey.Replace("BPM", string.Empty), float.Parse(infoValue));
						}
					} else if(txtLine.Contains(":")) {
						string[] keyValue = txtLine.Split(':');

						//string text4 = array3[0];
						int beat = int.Parse(keyValue[0].Substring(0, 3));
						string type = keyValue[0].Substring(3, 2);

						string value = keyValue[1];
						if(type == "02") {
							var jObject = new JObject();
							jObject["beat"] = beat;
							jObject["percent"] = float.Parse(value);
							notesPercent.Add(jObject);
							notesPercentDict[beat] = jObject;
						} else {
							int objLength = value.Length / 2;
							for(int i = 0; i < objLength; i++) {
								string note = value.Substring(i * 2, 2);
								if(note == "00") {
									continue;
								}
								float theTick = (float)i / (float)objLength + (float)beat;
								// "Variable speed"
								if(type == "03" || type == "08") {
									float freq = 60f / ((type != "08" || !bpmTones.ContainsKey(note)) ? ((float)Convert.ToInt32(note, 16)) : bpmTones[note]) * 4f;
									var jObject = new JObject();
									jObject["tick"] = theTick;
									jObject["freq"] = freq;
									list.Add(jObject);
									list.Sort((l, r) => {
										float num12 = (float)l["tick"];
										float num13 = (float)r["tick"];
										if(num12 > num13) {
											return -1;
										}
										return 1;
									});
								} else {
									float num3 = 0f;
									float num4 = 0f;
									var list2 = list.FindAll((JObject b) => (float)b["tick"] < theTick);
									for(int k = list2.Count - 1; k >= 0; k--) {
										var jobject5 = list2[k];
										float num5 = 0f;
										float num6 = (float)jobject5["freq"];
										if(k - 1 >= 0) {
											var jobject6 = list2[k - 1];
											num5 = (float)jobject6["tick"] - (float)jobject5["tick"];
										}
										if(k == 0) {
											num5 = theTick - (float)jobject5["tick"];
										}
										float num7 = num4;
										num4 += num5;
										int num8 = Mathf.FloorToInt(num7);
										int num9 = Mathf.CeilToInt(num4);
										for(int m = num8; m < num9; m++) {
											int index = m;
											float num10 = 1f;
											if(m == num8) {
												num10 = (float)(m + 1) - num7;
											}
											if(m == num9 - 1) {
												num10 = num4 - (float)(num9 - 1);
											}
											if(num9 == num8 + 1) {
												num10 = num4 - num7;
											}
											notesPercentDict.TryGetValue(index, out JToken jtoken);
											float num11 = (jtoken == null) ? 1f : ((float)jtoken["percent"]);
											num3 += (float)Mathf.RoundToInt(num10 * num11 * num6 / 1E-06f) * 1E-06f;
										}
									}
									var jobject7 = new JObject();
									jobject7["time"] = num3;
									jobject7["value"] = note;
									jobject7["tone"] = type;
									notes.Add(jobject7);
								}
							}
						}
					}
				}
			}

            notes.OrderBy((t) => {
                return 1;
            });

            var sorted = notes.ToList();
            sorted.Sort((l, r) => {
				var lTime = (double)((float)l["time"]);
				var rTime = (double)((float)r["time"]);
				var lTone = (string)l["tone"];
				var rTone = (string)r["tone"];

				// This should be accurate for note sorting up to 6 decimal places
				var lScore = ((long)(lTime * 1_000_000) * 10) + (lTone == "15" ? 0 : 1);
				var rScore = ((long)(rTime * 1_000_000) * 10) + (rTone == "15" ? 0 : 1);

				return Math.Sign(lScore - rScore);
			});

            notes = new JArray(sorted);

			var bms = new BMS {
				info = info,
				notes = notes,
				notesPercent = notesPercent,
				md5 = md5
			};
			bms.info["NAME"] = bmsName;
			bms.info["NEW"] = true;
			
			if(bms.info.Properties().ToList().Find((p) => p.Name == "BANNER") == null) {
				bms.info["BANNER"] = "cover/none_cover.png";
			} else {
				bms.info["BANNER"] = "cover/" + (string)bms.info["BANNER"];
			}
			return bms;
		}

        /// <summary>
        /// Compiles data from BMS into a StageInfo object.
        /// </summary>
        /// <param name="bms">BMS to load data from</param>
        /// <param name="info">StageInfo to load data into</param>
        public static void TransmuteData(BMS bms, StageInfo info) {
            if(musicDatas.Count > 0) musicDatas.Clear();
            if(noteDatasUid == null) InitNoteDatas();
            delay = 0;

            // Initial data load
            var jArray = bms.GetNoteDatas();
            Log.Debug("Got Data");

            // Placeholder Note
            Add(new MusicData());

            // Insertion Pass 1
            ProcessNotes(jArray);
            Sort();

            // Insertion Pass 2
            ProcessBossAnimationsNew(bms);
            //ProcessBossAnimationsOld(bms);
            ProcessDelay(bms);
            Sort();

            // Staging Pass
            ProcessGeminis();
            ApplyDelay();

            // Move MusicData into StageInfo's Il2Cpp list
            info.musicDatas = new Il2CppSystem.Collections.Generic.List<MusicData>();
            foreach(var m in musicDatas) {
                info.musicDatas.Add(m);
            }
            info.delay = delay;
            musicDatas.Clear();
            Log.Debug("Moved Notes and Delay to StageInfo");

            Log.Debug("Finished");
        }

        /// <summary>
        /// Converts the given StageInfo into JSON and saves it to the given path.
        /// </summary>
        /// <param name="info">StageInfo object (typically a modded chart)</param>
        /// <param name="outputPath">Path to output JSON file, including filename and extnesion</param>
        public static void CompileStageInfo(StageInfo info, string outputPath) {
            var json = new JObject() {
                { "bpm", info.bpm },
                { "delay", (float)info.delay },
                { "difficulty", info.difficulty },
                { "mapName", info.mapName },
                { "md5", info.md5 },
                { "music", info.music },
                { "name", info.name },
                { "scene", info.scene },
                { "dialogEvents", new JObject() },
                { "musicDatas", new JArray() },
                { "sceneEvents", new JArray() }
            };
            if(info.dialogEvents != null) {
                foreach(var kv in info.dialogEvents) {
                    var jArr = new JArray();
                    json["dialogEvents"][kv.Key] = jArr;
                    foreach(var v in kv.Value) {
                        jArr.Add(JsonConvert.SerializeObject(v, new DialogConverter()).JsonDeserialize<JObject>());
                    }
                }
            }
            if(info.musicDatas != null) {
                var jArr = json["musicDatas"] as JArray;
                foreach(var mData in info.musicDatas) {
                    jArr.Add(JsonConvert.SerializeObject(mData, new MusicDataConverter()).JsonDeserialize<JObject>());
                }
            }

            /*var stageInfoJson = new JObject();
            var dialogs = new Dictionary<string, List<GameDialogArgs>>();
            foreach(var kv in info.dialogEvents) {
                var list = new List<GameDialogArgs>();
                foreach(var args in kv.value) {
                    list.Add(args);
                }
                dialogs[kv.key] = list;
            }
            var jsonDialogs = new Newtonsoft.Json.Linq.JObject();*/

            File.WriteAllText(outputPath, json.JsonSerialize());
        }

        /// <summary>
        /// Adds MusicData to the list.
        /// Provides a cap of 32768 objects.
        /// </summary>
        /// <param name="mData"></param>
        private static void Add(MusicData mData) {
            if(musicDatas.Count <= short.MaxValue) {
                musicDatas.Add(mData);
            }
        }

        /// <summary>
        /// Sorts the MusicData by time and reassigns their objIds to match.
        /// Placeholder note (at index 0) is kept in place.
        /// </summary>
        private static void Sort() {
            // Prevent placeholder from being sorted
            musicDatas.RemoveAt(0);

            // Sort by tick
            musicDatas.Sort(delegate (MusicData l, MusicData r) {
                return (!(r.tick - r.dt - (l.tick - l.dt) > 0)) ? 1 : (-1);
            });

            // Re-insert placeholder
            musicDatas.Insert(0, new MusicData());

            // Reapply object IDs and round tick to nearest 3 decimal places
            for(int i = 0; i < musicDatas.Count; i++) {
                MusicData musicData = musicDatas[i];
                musicData.tick = Il2CppSystem.Decimal.Round(musicData.tick, 3);
                musicData.objId = (short)i;
                musicDatas[i] = musicData;
            }

            Log.Debug("Sorted");
        }

        /// <summary>
        /// Initializes NoteData cache.
        /// Speeds up future NoteData lookups (there are a lot of them).
        /// </summary>
        private static void InitNoteDatas() {
            noteDatasUid = new Dictionary<string, NoteConfigData>();
            noteDatasBoss = new Dictionary<string, Dictionary<string, NoteConfigData>>();

            foreach(var nData in SingletonScriptableObject<NoteDataMananger>.instance.noteDatas) {
                if(!noteDatasUid.ContainsKey(nData.uid)) {
                    noteDatasUid[nData.uid] = nData;
                }
                if(nData.type == 0 && !string.IsNullOrEmpty(nData.boss_action) && nData.boss_action != "0") {
                    if(!noteDatasBoss.ContainsKey(nData.scene)) {
                        noteDatasBoss[nData.scene] = new Dictionary<string, NoteConfigData>();
                    }
                    if(!noteDatasBoss[nData.scene].ContainsKey(nData.boss_action)) {
                        noteDatasBoss[nData.scene][nData.boss_action] = nData;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the BMS JSON into MusicData.
        /// Handles both standard notes and hold notes.
        /// </summary>
        /// <param name="jArray"></param>
        private static void ProcessNotes(JArray jArray) {
            short objIdx = 1;
            for(int i = 0; i < jArray.Count; i++) {
                MusicConfigData configData = JsonToConfigData((JObject)jArray[i]);
                if(configData.time < 0) {
                    continue;
                }

                // Create MusicData for the new note
                var newNote = new MusicData {
                    objId = objIdx++,
                    tick = Il2CppSystem.Decimal.Round(configData.time, 3),
                    configData = configData,
                    isLongPressEnd = false,
                    isLongPressing = false
                };
                if(noteDatasUid.TryGetValue(newNote.configData.note_uid, out var noteData)) {
                    newNote.noteData = noteData;
                }
                Add(newNote);

                // Create long note ticks
                // Long notes have invisible score ticks every 100ms while being held
                if(!newNote.isLongPressStart) {
                    continue;
                }
                int longPressCount = newNote.longPressCount;
                int endIndex = (int)(Il2CppSystem.Decimal.Round(newNote.tick + newNote.configData.length - newNote.noteData.left_great_range - newNote.noteData.left_perfect_range, 3) / (Il2CppSystem.Decimal)0.001f);
                for(int j = 1; j <= longPressCount; j++) {
                    var longNoteTick = new MusicData();
                    longNoteTick.objId = objIdx++;
                    longNoteTick.tick = newNote.tick + (Il2CppSystem.Decimal)0.1f * j;
                    longNoteTick.configData = newNote.configData;
                    var configData2 = longNoteTick.configData;
                    configData2.length = 0;
                    longNoteTick.configData = configData2;
                    longNoteTick.isLongPressing = true;
                    longNoteTick.isLongPressEnd = false;
                    longNoteTick.noteData = newNote.noteData;
                    longNoteTick.longPressPTick = newNote.configData.time;
                    longNoteTick.endIndex = endIndex;
                    if(j == longPressCount) {
                        longNoteTick.isLongPressing = false;
                        longNoteTick.isLongPressEnd = true;
                        longNoteTick.tick = newNote.tick + newNote.configData.length;
                    }
                    Add(longNoteTick);
                }
            }

            Log.Debug("Processed Notes");
        }

        /// <summary>
        /// Automatically inserts boss animations where required.
        /// </summary>
        /// <param name="bms"></param>
        private static void ProcessBossAnimationsNew(BMS bms) {
            var data = SingletonScriptableObject<NoteDataMananger>.instance.noteDatas;
            string scene = (string)bms.info["GENRE"];
            string configStringValue = Singleton<ConfigManager>.instance.GetConfigStringValue("boss", "scene_name", "boss_name", scene);
            SpineActionController component = ResourcesManager.instance.LoadFromName<GameObject>(configStringValue).GetComponent<SpineActionController>();
            ExposedList<Spine.Animation> animations = component.gameObject.GetComponent<SkeletonAnimation>().skeletonDataAsset.GetSkeletonData(quiet: true).Animations;

            float GetDuration(string anim) {
                var arr = new SkeletActionData[component.actionData.Count];
                component.actionData.CopyTo(arr, 0);
                var skeletActionData = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == anim);
                var animName = anim;
                if(skeletActionData != null && skeletActionData.actionIdx != null && skeletActionData.actionIdx.Length != 0) {
                    animName = skeletActionData.actionIdx[0];
                }
                return animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == animName)).Duration;
            }

            float GetStartDelay(string prefab) {
                return ResourcesManager.instance.LoadFromName<GameObject>(prefab).GetComponent<SpineActionController>().startDelay;
            }

            var bossAnimDatas = new List<MusicData>();
            foreach(var mData in musicDatas) {
                if(mData.isBossNote) {
                    bossAnimDatas.Add(mData);
                }
            }

            // IF the boss isn't used at all for some reason, we have no reason to be here
            if(bossAnimDatas.Count == 0) return;

            // Insert a boss exit at the end of the chart if boss doesn't leave
            var finalAnim = bossAnimDatas[bossAnimDatas.Count - 1];
            if(animStatesRight[finalAnim.noteData.boss_action] != BossState.OFFSCREEN) {
                var finalNote = musicDatas[musicDatas.Count - 1];
                var tick = finalNote.tick;

                if(finalNote.isBossNote) {
                    var startDelay = Il2CppSystem.Decimal.Round((Il2CppSystem.Decimal)GetStartDelay(finalNote.noteData.prefab_name), 3);
                    var duration = (Il2CppSystem.Decimal)GetDuration(finalNote.noteData.boss_action);
                    tick += Il2CppSystem.Decimal.Round(-startDelay + duration, 3);
                }

                tick = Il2CppSystem.Decimal.Round(tick + (Il2CppSystem.Decimal)0.1f, 3);
                NoteConfigData exitData = noteDatasBoss[scene]["out"];
                var exitConfig = new MusicConfigData {
                    note_uid = exitData.uid,
                    time = tick
                };
                var exitMData = new MusicData {
                    objId = 0,
                    tick = exitConfig.time,
                    configData = exitConfig,
                    noteData = exitData
                };
                Add(exitMData);
                bossAnimDatas.Add(exitMData);
                Log.Debug($"Added out at " + exitConfig.time.ToString());
            }

            // Fixes boss gears in incorrect phase.
            // Switches a boss gear to the most accurate phase based on surrounding animations.
            // Note that MANUAL PHASE SWITCH may still be required in some scenarios.
            var phaseGearConfig = new NoteConfigData() { ibms_id = "" };
            for(int i = 0; i < bossAnimDatas.Count; i++) {
                var currData = bossAnimDatas[i];
                if((NoteType)currData.noteData.type == NoteType.Block) {
                    // Find the next boss animation that is not a gear
                    var bossAnimAhead = new MusicData() { configData = new MusicConfigData() { time = Il2CppSystem.Decimal.MinValue } };
                    for(int j = i + 1; j < bossAnimDatas.Count; j++) {
                        var jData = bossAnimDatas[j];
                        if(jData.noteData.type != 2) {
                            bossAnimAhead = jData;
                            break;
                        }
                    }

                    // Previous animation.
                    // This one CAN be gears, because they've already been resolved
                    var bossAnimBefore = i > 0 ? bossAnimDatas[i - 1] : new MusicData() { configData = new MusicConfigData() { time = Il2CppSystem.Decimal.MinValue } };

                    // Base the gear's new phase on the data that is closest in time to this gear
                    MusicData dataToUse;
                    var ahead = false;
                    var diffToAhead = Math.Abs((float)currData.configData.time - (float)bossAnimAhead.configData.time);
                    var diffToBefore = Math.Abs((float)currData.configData.time - (float)bossAnimBefore.configData.time);
                    if(diffToAhead < diffToBefore) {
                        dataToUse = bossAnimAhead;
                        ahead = true;
                    } else dataToUse = bossAnimBefore;

                    // Determine if the state chosen is actually valid (boss is in phase 1 or 2)
                    // Choose the other side instead if it is not valid
                    var stateBehind = i > 0 ? animStatesRight[bossAnimBefore.noteData.boss_action] : BossState.OFFSCREEN;
                    var stateAhead = animStatesLeft.ContainsKey(bossAnimAhead.noteData.boss_action) ? animStatesLeft[bossAnimAhead.noteData.boss_action] : BossState.OFFSCREEN;
                    var usedState = ahead ? stateAhead : stateBehind;
                    var correctState = usedState == BossState.PHASE_1 || usedState == BossState.PHASE_2;
                    if(!correctState) {
                        ahead = !ahead;
                        usedState = ahead ? stateAhead : stateBehind;
                        correctState = usedState == BossState.PHASE_1 || usedState == BossState.PHASE_2;
                    }

                    // If neither surrounding state is valid, don't bother
                    if(!correctState) {
                        continue;
                    }

                    // Determine the phase of the chosen data to base upon
                    var bossPhase = usedState == BossState.PHASE_1 ? 1 : 2;

                    // If the found data doesn't match up with a phase, then don't bother (it was a boss enter or exit or something weird)
                    // In cases like these, the automatic selection was likely more accurate
                    if(bossPhase == 0) continue;

                    // Don't replace if the gear is already in the wanted phase
                    if((ahead && animStatesLeft[currData.noteData.boss_action] == usedState) 
                        || (!ahead && animStatesRight[currData.noteData.boss_action] == usedState)) {
                        continue;
                    }

                    // Find the corresponding NoteConfigData that matches this gear's data and has the desired attack pattern.
                    if(phaseGearConfig.ibms_id != currData.noteData.ibms_id
                        || phaseGearConfig.pathway != currData.noteData.pathway
                        || phaseGearConfig.scene != currData.noteData.scene
                        || phaseGearConfig.speed != currData.noteData.speed
                        || !phaseGearConfig.boss_action.StartsWith($"boss_far_atk_{bossPhase}")) {
                        foreach(var nData in data) {
                            if(nData.ibms_id == currData.noteData.ibms_id
                                && nData.pathway == currData.noteData.pathway
                                && nData.scene == currData.noteData.scene
                                && nData.speed == currData.noteData.speed
                                && nData.boss_action.StartsWith($"boss_far_atk_{bossPhase}")) {
                                phaseGearConfig = nData;
                                break;
                            }
                        }
                    }

                    // Replace the gear in the list with one that has the updated config.
                    var realGear = new MusicData {
                        objId = currData.objId,
                        tick = currData.tick,
                        configData = new MusicConfigData {
                            blood = currData.configData.blood,
                            id = currData.configData.id,
                            length = currData.configData.length,
                            note_uid = phaseGearConfig.uid,
                            pathway = currData.configData.pathway,
                            time = currData.configData.time
                        },
                        isLongPressEnd = currData.isLongPressEnd,
                        isLongPressing = currData.isLongPressing,
                        noteData = phaseGearConfig
                    };

                    musicDatas[realGear.objId] = realGear;
                    bossAnimDatas[i] = realGear;
                    Log.Debug("Fixing gear phase at " + currData.tick.ToString() + ", phase " + bossPhase + ", uid: " + phaseGearConfig.uid + $", ahead: {ahead}, action: {dataToUse.noteData.boss_action}");
                    continue;
                }
            }

            // Run through the list resolving state changes
            var bossState = BossState.OFFSCREEN;
            for(var i = 0; i < bossAnimDatas.Count; i++) {
                var anim = bossAnimDatas[i].noteData.boss_action;
                var nextState = animStatesLeft[anim];

                if(bossState != nextState) {
                    var transfer = stateTransferAnims[bossState][nextState];
                    var transferNoteData = noteDatasBoss[scene][transfer];
                    var alignment = transferAlignment[transfer];

                    var alignData = alignment == AnimAlignment.RIGHT ? bossAnimDatas[i] : bossAnimDatas[i - 1];

                    var rightDelay = (Il2CppSystem.Decimal)GetStartDelay(bossAnimDatas[i].noteData.prefab_name);
                    var leftDelay = i == 0 ? 0 : (Il2CppSystem.Decimal)GetStartDelay(bossAnimDatas[i - 1].noteData.prefab_name);
                    var alignDelay = alignment == AnimAlignment.LEFT ? leftDelay : rightDelay;

                    //var durationLeft = i == 0 ? 0 : (Il2CppSystem.Decimal)GetDuration(bossAnimDatas[i - 1].noteData.boss_action);
                    var duration = (Il2CppSystem.Decimal)GetDuration(transfer);

                    // Whatever happens, anim cannot start before the left animation (or it will appear out of order)
                    /*var leftBound = i == 0 ? Il2CppSystem.Decimal.MinValue : bossAnimDatas[i - 1].tick + (Il2CppSystem.Decimal)0.001f;
                    var time = Il2CppSystem.Decimal.Max(leftBound - leftDelay, alignData.tick - startDelay - duration);
                    time = Il2CppSystem.Decimal.Round(time, 3);*/

                    var mConfig = new MusicConfigData {
                        note_uid = transferNoteData.uid,
                        time = Il2CppSystem.Decimal.Round(alignData.tick - alignDelay - (duration * (int)alignment), 3)
                    };
                    var mData = new MusicData {
                        tick = mConfig.time,
                        configData = mConfig,
                        noteData = transferNoteData
                    };

                    // Don't create boss animations when there isn't enough space
                    // MusicData hasn't been offset for start delay yet
                    // Tolerates a certain amount of animation clipping from the right
                    var tolerance = (Il2CppSystem.Decimal)0.300f;
                    /*var endTick = Il2CppSystem.Decimal.Round(alignData.configData.time - alignDelay + Il2CppSystem.Decimal.Abs(duration), 3);
                    var fitsRight = endTick < bossAnimDatas[i].tick - rightDelay + tolerance;*/
                    var fits = false;
                    switch(alignment) {
                        case AnimAlignment.LEFT:
                            // When coming from the left, ensure there is enough time for the animation to run properly
                            fits = mData.tick + duration < bossAnimDatas[i].tick - rightDelay + tolerance;
                            break;
                        case AnimAlignment.RIGHT:
                            // When coming from the right, ensure the animation won't arrive before the one on the left
                            fits = i == 0 || mData.tick > bossAnimDatas[i - 1].tick - leftDelay;
                            break;
                    }
                    if(!fits) {
                        Log.Debug($"SKIP  {transfer} at {mConfig.time.ToString()}, state: {bossState} | next: {nextState}");
                        bossState = animStatesRight[bossAnimDatas[i].noteData.boss_action];
                        continue;
                    }
                    /*if(alignment == AnimAlignment.LEFT) {
                        fitsRight = bossAnimDatas[i].tick - bossAnimDatas[i - 1].tick >= Il2CppSystem.Decimal.Abs(duration) - tolerance;
                    }
                    if(alignment == AnimAlignment.RIGHT) {

                    }
                    if(!fitsRight) {
                        MelonLoader.MelonLogger.Msg($"(!) SKIP  {transfer} at {mConfig.time.ToString()}, state: {bossState} | next: {nextState} | endTick: {endTick.ToString()}");
                        bossState = animStatesRight[bossAnimDatas[i].noteData.boss_action];
                        continue;
                    }*/

                    // Insert new animation to the notes list
                    Add(mData);

                    // Insert new animation into the animations list
                    bossAnimDatas.Insert(i, mData);
                    i--;

                    Log.Debug($"Added {transfer} at {mConfig.time.ToString()}, state: {bossState} | next: {nextState}");
                } else {
                    bossState = animStatesRight[bossAnimDatas[i].noteData.boss_action];
                }
            }
        }

        /// <summary>
        /// Automatically inserts boss animations where required.
        /// OLD VERSION, for comparison purposes only!
        /// </summary>
        /// <param name="bms"></param>
        private static void ProcessBossAnimationsOld(BMS bms) {
            var data = SingletonScriptableObject<NoteDataMananger>.instance.noteDatas;
            string text = (string)bms.info["GENRE"];
            string configStringValue = Singleton<ConfigManager>.instance.GetConfigStringValue("boss", "scene_name", "boss_name", text);
            SpineActionController component = ResourcesManager.instance.LoadFromName<GameObject>(configStringValue).GetComponent<SpineActionController>();
            ExposedList<Spine.Animation> animations = component.gameObject.GetComponent<SkeletonAnimation>().skeletonDataAsset.GetSkeletonData(quiet: true).Animations;
            string currAction = "0";
            MusicData musicData = new MusicData();
            ArrayList arrayList = new ArrayList(musicDatas);
            int num2 = -1;
            MusicConfigData musicConfigData;

            // Fixes boss gears in incorrect phase.
            // Switches a boss gear from phase 1 (default) to phase 2 under the following conditions:
            // - The previous boss animation was an animation indicating the boss is now in PHASE 2.
            //
            // Note that MANUAL PHASE SWITCH is still required in some scenarios.
            var bossAnimBefore = new MusicData() { configData = new MusicConfigData() { time = Il2CppSystem.Decimal.MinValue } };
            NoteConfigData phaseGearConfig = new NoteConfigData() { ibms_id = "" };
            /*for(int i = 0; i < arrayList.Count; i++) {
                var currData = (MusicData)arrayList[i];
                //MelonLoader.MelonLogger.Msg(string.Format("{0,5} {1,5} {2,20}", bossPhase, currData.noteData.type, currData.noteData.boss_action));
                if(currData.noteData.type == 2 && (currData.noteData.boss_action == "boss_far_atk_1_R" || currData.noteData.boss_action == "boss_far_atk_1_L" || currData.noteData.boss_action == "boss_far_atk_2")) {

                    // Find the next boss animation that is not a gear (variable) or has no animation (doesn't matter)
                    var bossAnimAhead = new MusicData() { configData = new MusicConfigData() { time = Il2CppSystem.Decimal.MinValue } };
                    for(int j = i; j < arrayList.Count; j++) {
                        var jData = (MusicData)arrayList[j];
                        // Ignore other Gears or notes without boss animations
                        // As Gears have variable phasing, they cannot be used as markers
                        if(jData.noteData.type != 2 && jData.noteData.boss_action != "0" && !string.IsNullOrEmpty(jData.noteData.boss_action)) {
                            bossAnimAhead = jData;
                            break;
                        }
                    }

                    // Base the gear's new phase on the data that is closest in time to this gear
                    MusicData dataToUse;
                    var ahead = false;
                    var diffToAhead = Math.Abs((float)currData.configData.time - (float)bossAnimAhead.configData.time);
                    var diffToBefore = Math.Abs((float)currData.configData.time - (float)bossAnimBefore.configData.time);
                    if(diffToAhead < diffToBefore) {
                        dataToUse = bossAnimAhead;
                        ahead = true;
                    } else dataToUse = bossAnimBefore;

                    // Determine the phase of the chosen data to base upon
                    var bossPhase = 0;
                    if(dataToUse.noteData.boss_action == "boss_far_atk_1_L"
                        || dataToUse.noteData.boss_action == "boss_far_atk_1_R") {
                        bossPhase = 1;
                        //MelonLoader.MelonLogger.Msg("Boss entered phase 1 at " + currData.tick.ToString());
                    }
                    if(dataToUse.noteData.boss_action == "boss_far_atk_2") {
                        bossPhase = 2;
                        //MelonLoader.MelonLogger.Msg("Boss entered phase 2 at " + currData.tick.ToString());
                    }

                    if(dataToUse.noteData.boss_action == "boss_far_atk_1_start"
                        || dataToUse.noteData.boss_action == "atk_1_to_2") {
                        bossPhase = ahead ? 1 : 2;
                    }

                    if(dataToUse.noteData.boss_action == "boss_far_atk_2_start"
                        || dataToUse.noteData.boss_action == "atk_2_to_1") {
                        bossPhase = ahead ? 2 : 1;
                    }

                    // If the found data doesn't match up with a phase, then don't bother (it was a boss enter or exit or something weird)
                    // In cases like these, the automatic selection was likely more accurate
                    if(bossPhase == 0) continue;

                    // Find the corresponding NoteConfigData that matches this gear's data and has the desired attack pattern.
                    if(phaseGearConfig.ibms_id != currData.noteData.ibms_id
                        || phaseGearConfig.pathway != currData.noteData.pathway
                        || phaseGearConfig.scene != currData.noteData.scene
                        || phaseGearConfig.speed != currData.noteData.speed
                        || !phaseGearConfig.boss_action.StartsWith($"boss_far_atk_{bossPhase}")) {
                        foreach(var nData in data) {
                            if(nData.ibms_id == currData.noteData.ibms_id
                                && nData.pathway == currData.noteData.pathway
                                && nData.scene == currData.noteData.scene
                                && nData.speed == currData.noteData.speed
                                && nData.boss_action.StartsWith($"boss_far_atk_{bossPhase}")) {
                                phaseGearConfig = nData;
                                break;
                            }
                        }
                    }

                    // Don't replace if the gear is already in the wanted phase
                    if(phaseGearConfig.boss_action == currData.noteData.boss_action) {
                        continue;
                    }

                    // Replace the gear in the list with one that has the updated config.
                    var realGear = new MusicData {
                        objId = currData.objId,
                        tick = currData.tick,
                        configData = new MusicConfigData {
                            blood = currData.configData.blood,
                            id = currData.configData.id,
                            length = currData.configData.length,
                            note_uid = phaseGearConfig.uid,
                            pathway = currData.configData.pathway,
                            time = currData.configData.time
                        },
                        isLongPressEnd = currData.isLongPressEnd,
                        isLongPressing = currData.isLongPressing,
                        noteData = phaseGearConfig
                    };
                    *//*realGear.noteData = phaseGearConfig;
                    realGear.configData = new MusicConfigData() {
                        blood = realGear.configData.blood,
                        id = realGear.configData.id,
                        length = realGear.configData.length,
                        note_uid = phaseGearConfig.uid,
                        pathway = realGear.configData.pathway,
                        time = realGear.configData.time
                    };*//*
                    //var realConfig = realGear.configData;
                    //realConfig.id = int.Parse(phaseGearConfig.id);
                    //var phaseGearUid = phaseGearConfig.uid;
                    //realConfig.note_uid = phaseGearUid;
                    //realGear.configData = realConfig;
                    musicDatas[i] = realGear;
                    arrayList[i] = realGear;
                    MelonLoader.MelonLogger.Msg("Fixing gear phase at " + currData.tick.ToString() + ", phase " + bossPhase + ", uid: " + phaseGearConfig.uid + $", ahead: {ahead}, action: {dataToUse.noteData.boss_action}");
                    //MelonLoader.MelonLogger.Msg("Mic check: " + musicDatas[i].noteData.boss_action);
                    //MelonLoader.MelonLogger.Msg("Mic check: " + ((MusicData)arrayList[i]).noteData.boss_action);
                    continue;
                }
                if(currData.noteData.boss_action != "0" && !string.IsNullOrEmpty(currData.noteData.boss_action)) {
                    bossAnimBefore = currData;
                }
            }*/

            // Steps backwards through the MusicData.
            // Inserts a PHASE 1 END or PHASE 2 END animation ONCE under the following conditions:
            // - The final boss animation in the chart is a PHASE 1 ATTACK or PHASE 2 ATTACK animation
            // - (Always true) The following MusicData does NOT have an ATTACK animation of the opposite PHASE
            //
            // The resulting new MusicData will have the following additional properties:
            // - Timing based on the current MusicData, accounting for note delay and the original scene's boss animation time
            for(int i = arrayList.Count - 1; i >= 0; i--) {
                MusicData current = (MusicData)arrayList[i];
                string nextAction = string.Empty;
                if(i + 1 < arrayList.Count) {
                    nextAction = ((MusicData)arrayList[i + 1]).noteData.boss_action;
                }
                string bossAction = current.noteData.boss_action;
                if(bossAction != "0" && !string.IsNullOrEmpty(bossAction)) {
                    string newAction = string.Empty;
                    if((bossAction == "boss_far_atk_1_L" || bossAction == "boss_far_atk_1_R") && nextAction != "boss_far_atk_2") {
                        newAction = "boss_far_atk_1_end";
                    }
                    if(bossAction == "boss_far_atk_2" && nextAction != "boss_far_atk_1_L" && nextAction != "boss_far_atk_1_R") {
                        newAction = "boss_far_atk_2_end";
                    }
                    if(!string.IsNullOrEmpty(newAction)) {
                        NoteConfigData noteData = noteDatasBoss[text][newAction];
                        Il2CppSystem.Decimal num4 = 0;
                        try {
                            num4 = (Il2CppSystem.Decimal)ResourcesManager.instance.LoadFromName<GameObject>(current.noteData.prefab_name).GetComponent<SpineActionController>().startDelay;
                        } catch(Exception) {
                            Debug.LogError(current.noteData.prefab_name);
                            throw;
                        }
                        num4 = Il2CppSystem.Decimal.Round(num4, 3);
                        var arr = new SkeletActionData[component.actionData.Count];
                        component.actionData.CopyTo(arr, 0);
                        var animName4 = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == bossAction).actionIdx[0];
                        Il2CppSystem.Decimal d3 = (Il2CppSystem.Decimal)animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == animName4)).Duration;
                        musicConfigData = new MusicConfigData {
                            id = 0,
                            length = 0,
                            note_uid = noteData.uid,
                            time = Il2CppSystem.Decimal.Round(current.tick - num4 + d3, 3)
                        };
                        musicData = new MusicData {
                            objId = 0,
                            tick = musicConfigData.time,
                            configData = musicConfigData,
                            noteData = noteData
                        };
                        Add(musicData);
                        Log.Debug($"(1) Added {newAction} at " + musicConfigData.time.ToString());
                    }
                    break;
                }
            }
            Log.Debug("Applied Phase End (in Reverse)");
            // Steps forwards through the MusicData.
            // Ignores MusicData that doesn't have a boss animation.
            // Inserts a variety of boss animations based on certain conditions.
            for(int k = 0; k < arrayList.Count; k++) {
                MusicData musicData6 = (MusicData)arrayList[k];
                string boss_action = musicData6.noteData.boss_action;

                // Skips this MusicData under the following conditions:
                // - The current MusicData does NOT have a boss animation
                if(!(boss_action != "0") || string.IsNullOrEmpty(boss_action)) {
                    continue;
                }
                num2 = ((!(boss_action == "out") && !(musicData6.noteData.ibms_id == "17")) ? (-1) : musicData6.objId);
                MusicData musicData7 = new MusicData();
                for(int num5 = k - 1; num5 >= 0; num5--) {
                    MusicData musicData8 = (MusicData)arrayList[num5];
                    if(musicData8.noteData.boss_action != "0") {
                        musicData7 = musicData8;
                        break;
                    }
                }
                string prevAction = currAction;
                currAction = boss_action;
                NoteConfigData noteData2 = musicData6.noteData;
                bool flag = false;
                Il2CppSystem.Decimal d4 = (Il2CppSystem.Decimal)ResourcesManager.instance.LoadFromName<GameObject>(musicData6.noteData.prefab_name).GetComponent<SpineActionController>().startDelay;
                d4 = Il2CppSystem.Decimal.Round(d4, 3);
                string action = string.Empty;
                if((prevAction == "boss_far_atk_1_L" || prevAction == "boss_far_atk_1_R")
                    && currAction != "atk_1_to_2"
                    && currAction != "atk_2_to_1"
                    && currAction != "boss_far_atk_1_start"
                    && currAction != "boss_far_atk_1_end"
                    && currAction != "boss_far_atk_1_L"
                    && currAction != "boss_far_atk_1_R"
                    && currAction != "boss_far_atk_2") {
                    action = "boss_far_atk_1_end";
                }
                if(prevAction == "boss_far_atk_2"
                    && currAction != "atk_1_to_2"
                    && currAction != "atk_2_to_1"
                    && currAction != "boss_far_atk_2_start"
                    && currAction != "boss_far_atk_2_end"
                    && currAction != "boss_far_atk_2"
                    && currAction != "boss_far_atk_1_L"
                    && currAction != "boss_far_atk_1_R") {
                    action = "boss_far_atk_2_end";
                }
                if(!string.IsNullOrEmpty(action)) {
                    if(noteDatasBoss.TryGetValue(text, out var dict) && dict.TryGetValue(action, out var nData)) {
                        noteData2 = nData;
                        flag = true;
                    }
                }

                // Inserts a PHASE 1 or PHASE 2 END animation under the following conditions:
                // - The previous MusicData has a PHASE 1 or PHASE 2 attack animation
                // - The current MusicData does NOT have any ATTACK animation, or a PHASE END animation of the same phase
                //
                // The resulting new MusicData will have the following additional properties:
                // - Timing based on the PREVIOUS MusicData, accounting for note delay and the original scene's boss animation time
                // - Note delay time is SUBTRACTED: Animation is intended to occur at the SAME TIME as the note
                // - Boss animation time is ADDED: Animation is intended to occur AFTER the note's boss animation
                if(flag) {
                    d4 = (Il2CppSystem.Decimal)ResourcesManager.instance.LoadFromName<GameObject>(musicData7.noteData.prefab_name).GetComponent<SpineActionController>().startDelay;
                    d4 = Il2CppSystem.Decimal.Round(d4, 3);
                    var arr = new SkeletActionData[component.actionData.Count];
                    component.actionData.CopyTo(arr, 0);
                    var findRes = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == action).actionIdx;
                    if(findRes == null || findRes.Length == 0) {
                        MelonLoader.MelonLogger.Warning("Can not find 'SkeletonActionData' of boss [{0}]", prevAction);
                    }
                    Il2CppSystem.Decimal d5 = (Il2CppSystem.Decimal)animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == findRes[0])).Duration;
                    musicConfigData = new MusicConfigData();
                    musicConfigData.id = 0;
                    musicConfigData.length = 0;
                    musicConfigData.note_uid = noteData2.uid;
                    musicConfigData.time = Il2CppSystem.Decimal.Round(musicData7.tick - d4 + d5, 3);
                    MusicConfigData configData3 = musicConfigData;
                    musicData = new MusicData();
                    musicData.objId = 0;
                    musicData.tick = configData3.time;
                    musicData.configData = configData3;
                    musicData.noteData = noteData2;
                    MusicData musicData9 = musicData;
                    Add(musicData9);
                    Log.Debug($"(2) Added {action} at " + musicConfigData.time.ToString() + ", curr: " + currAction + " | prev: " + prevAction);
                }
                action = string.Empty;
                flag = false;

                // Inserts an ENTRANCE animation under the following conditions:
                // - The previous MusicData has NO animation, an EXIT animation, or is a BOSS MASHER 2 (situations where the boss is off-screen)
                // - The current MusicData does NOT have an ENTRANCE animation
                //
                // The resulting new MusicData will have the following additional properties:
                // - Timing based on the CURRENT MusicData, accounting for note delay and the original scene's boss animation time
                // - Note delay time is SUBTRACTED: Animation is intended to occur at the SAME TIME as the note
                // - Boss animation time is SUBTRACTED: Animation is intended to occur BEFORE the note's boss animation
                if((prevAction == "0" || prevAction == "out" || musicData7.noteData.ibms_id == "17") && currAction != "in") {
                    action = "in";
                    noteData2 = noteDatasBoss[text][action];
                    var arr = new SkeletActionData[component.actionData.Count];
                    component.actionData.CopyTo(arr, 0);
                    var animName3 = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == action).actionIdx[0];

                    Il2CppSystem.Decimal d6 = (Il2CppSystem.Decimal)animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == animName3)).Duration;
                    musicConfigData = new MusicConfigData();
                    musicConfigData.id = 0;
                    musicConfigData.length = 0;
                    musicConfigData.note_uid = noteData2.uid;
                    musicConfigData.time = Il2CppSystem.Decimal.Round(musicData6.tick - d4 - d6, 3);
                    MusicConfigData configData4 = musicConfigData;
                    musicData = new MusicData();
                    musicData.objId = 0;
                    musicData.tick = configData4.time;
                    musicData.configData = configData4;
                    musicData.noteData = noteData2;
                    MusicData musicData10 = musicData;
                    Add(musicData10);
                    Log.Debug($"(3) Added {action} at " + musicConfigData.time.ToString() + ", curr: " + currAction + " | prev: " + prevAction);
                }

                if((currAction == "boss_far_atk_1_L" || currAction == "boss_far_atk_1_R")
                    && prevAction != "atk_1_to_2"
                    && prevAction != "atk_2_to_1"
                    && prevAction != "boss_far_atk_1_start"
                    && prevAction != "boss_far_atk_1_end"
                    && prevAction != "boss_far_atk_1_L"
                    && prevAction != "boss_far_atk_1_R") {
                    action = ((prevAction == "boss_far_atk_2") ? "atk_2_to_1" : "boss_far_atk_1_start");
                } else if(currAction == "boss_far_atk_2"
                    && prevAction != "atk_1_to_2"
                    && prevAction != "atk_2_to_1"
                    && prevAction != "boss_far_atk_2_start"
                    && prevAction != "boss_far_atk_2_end"
                    && prevAction != "boss_far_atk_2") {
                    action = ((prevAction == "boss_far_atk_1_L" || prevAction == "boss_far_atk_1_R") ? "atk_1_to_2" : "boss_far_atk_2_start");
                }
                if(!string.IsNullOrEmpty(action)) {
                    if(noteDatasBoss.TryGetValue(text, out var dict) && dict.TryGetValue(action, out var nData)) {
                        noteData2 = nData;
                        flag = true;
                    }
                }

                // Inserts a PHASE 1 START, PHASE 2 START, PHASE 1 -> 2, or PHASE 2 -> 1 animation under the following conditions:
                // - The current MusicData has a PHASE 1 ATTACK or PHASE 2 ATTACK animation
                // - The previous MusicData does NOT have a START or ATTACK animation of the same phase
                // - A PHASE SWITCH animation will be used if the previous MusicData has a ATTACK animation of the opposite phase
                // - A START animation will be used otherwise
                //
                // The resulting new MusicData will have the following additional properties:
                // - Timing based on the CURRENT MusicData, accounting for note delay and the original scene's boss animation time
                // - Note delay time is SUBTRACTED: Animation is intended to occur at the SAME TIME as the note
                // - Boss animation time is SUBTRACTED: Animation is intended to occur BEFORE the note's boss animation
                if(flag) {
                    var arr = new SkeletActionData[component.actionData.Count];
                    component.actionData.CopyTo(arr, 0);
                    var actionIdx = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == action).actionIdx;
                    if(actionIdx == null || actionIdx.Length == 0) {
                        MelonLoader.MelonLogger.Warning("Can not find 'SkeletonActionData' of boss [{0}]", action);
                    }
                    string animName2 = actionIdx[0];
                    Il2CppSystem.Decimal d7 = (Il2CppSystem.Decimal)animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == animName2)).Duration;
                    musicConfigData = new MusicConfigData();
                    musicConfigData.id = 0;
                    musicConfigData.length = 0;
                    musicConfigData.note_uid = noteData2.uid;
                    musicConfigData.time = Il2CppSystem.Decimal.Round(musicData6.tick - d4 - d7, 3);
                    MusicConfigData configData5 = musicConfigData;
                    musicData = new MusicData();
                    musicData.objId = 0;
                    musicData.tick = configData5.time;
                    musicData.configData = configData5;
                    musicData.noteData = noteData2;
                    MusicData musicData11 = musicData;
                    Add(musicData11);
                    Log.Debug($"(4) Added {action} at " + musicConfigData.time.ToString() + ", curr: " + currAction + " | prev: " + prevAction);
                    //MelonLoader.MelonLogger.Msg($"You're kidding me right? " + );
                }
            }
            //MelonLoader.MelonLogger.Msg("Applied Everything Else (forwards)");
            // Inserts an EXIT animation after the final note under the following conditions:
            // - The final MusicData with a boss animation is NOT an EXIT animation or BOSS MASHER 2
            // - There exists any MusicData with any boss animation
            //
            // The resulting new MusicData will have the following additional properties:
            // - Timing based on the CURRENT MusicData, accounting for note delay and the original scene's boss animation time
            // - Note delay time is SUBTRACTED: Animation is intended to occur at the SAME TIME as the note
            // - Boss animation time is ADDED: Animation is intended to occur AFTER the note's boss animation (if present)
            // - An additional offset of 100ms is added
            // - Unlike other forms of automatic boss animations, this can stack with the automatically added phase end at end of chart
            if(num2 == -1 && musicDatas.Exists((MusicData d) => d.noteData.boss_action != "0" && !string.IsNullOrEmpty(d.noteData.boss_action))) {
                List<MusicData> list = new List<MusicData>(musicDatas);
                MusicData myMd = new MusicData();
                //Il2CppSystem.Decimal max = 0;
                Il2CppSystem.Decimal num6 = 0;
                list.ForEach((d) => {
                    if(d.tick > num6) num6 = d.tick;
                });
                /*Il2CppSystem.Decimal num6 = list.FindLast((d) => {
                    if(d.tick > max) {
                        max = d.tick;
                        return true;
                    }
                    return false;
                }).tick;*/
                foreach(MusicData item6 in list) {
                    if(!(item6.tick != num6)) {
                        myMd = item6;
                        break;
                    }
                }
                //MelonLoader.MelonLogger.Msg($"Placing boss exit at {num6}, which is totally {myMd.ToString()}");
                if(myMd.noteData.boss_action != "0") {
                    Il2CppSystem.Decimal d8 = (Il2CppSystem.Decimal)ResourcesManager.instance.LoadFromName<GameObject>(myMd.noteData.prefab_name).GetComponent<SpineActionController>().startDelay;
                    d8 = Il2CppSystem.Decimal.Round(d8, 3);
                    string animName = myMd.noteData.boss_action;
                    var arr = new SkeletActionData[component.actionData.Count];
                    component.actionData.CopyTo(arr, 0);
                    var skeletActionData = new List<SkeletActionData>(arr).Find((SkeletActionData dd) => dd.name == myMd.noteData.boss_action);
                    if(skeletActionData.actionIdx != null && skeletActionData.actionIdx.Length != 0) {
                        animName = skeletActionData.actionIdx[0];
                    }
                    Il2CppSystem.Decimal d9 = (Il2CppSystem.Decimal)animations.Find((Il2CppSystem.Predicate<Spine.Animation>)((Spine.Animation a) => a.Name == animName)).Duration;
                    num6 += Il2CppSystem.Decimal.Round(-d8 + d9, 3);
                }
                num6 = Il2CppSystem.Decimal.Round(num6 + (Il2CppSystem.Decimal)0.1f, 3);
                NoteConfigData noteData3 = noteDatasBoss[text]["out"];
                musicConfigData = new MusicConfigData();
                musicConfigData.id = 0;
                musicConfigData.length = 0;
                musicConfigData.note_uid = noteData3.uid;
                musicConfigData.time = num6;
                MusicConfigData configData6 = musicConfigData;
                musicData = new MusicData();
                musicData.objId = 0;
                musicData.tick = configData6.time;
                musicData.configData = configData6;
                musicData.noteData = noteData3;
                MusicData musicData12 = musicData;
                Add(musicData12);
                Log.Debug($"(5) Added out at " + musicConfigData.time.ToString());
            }
            Log.Debug($"Processed Boss Animations");
        }

        /// <summary>
        /// Calculates the starting delay of the chart.
        /// Also sets MusicData dt to that of the prefab used at the time of hitting it.
        /// </summary>
        private static void ProcessDelay(BMS bms) {
            var delayCache = new Dictionary<string, Il2CppSystem.Decimal>();
            var scene = (string)bms.info["GENRE"];
            var sceneIdx = int.Parse(scene.Split('_')[1]);
            var sceneInfo = Singleton<StageBattleComponent>.instance.sceneInfo;

            for(int i = 0; i < musicDatas.Count; i++) {
                var mData = musicDatas[i];
                var type = (NoteType)mData.noteData.type;
                if(!string.IsNullOrEmpty(mData.noteData.ibms_id)) {
                    if(type == NoteType.SceneChange) {
                        sceneIdx = sceneInfo[mData.noteData.ibms_id];
                    }

                    var prefabName = mData.noteData.prefab_name;

                    if(!string.IsNullOrEmpty(prefabName)) {
                        // If not a pickup type, convert to most recent scene
                        // Scene-agnostic, "empty_", and "boss_" types don't convert in this way
                        // WARNING: WILL BREAK IF PPG ADDS 10th SCENE
                        if(type != NoteType.Hp && type != NoteType.Music) {
                            char c = prefabName[1];
                            if(c != '0' && c != 'o' && c != 'm') {
                                prefabName = prefabName.Remove(1, 1).Insert(1, sceneIdx.ToString());
                            }
                        }

                        // Load start delay from object or cache
                        if(!delayCache.ContainsKey(prefabName)) {
                            GameObject gameObject2 = ResourcesManager.instance.LoadFromName<GameObject>(prefabName);

                            if(gameObject2 != null) {
                                SpineActionController component2 = gameObject2.GetComponent<SpineActionController>();
                                delayCache[prefabName] = (Il2CppSystem.Decimal)component2.startDelay;
                            }
                        }

                        // Apply start delay to dt
                        if(delayCache.TryGetValue(prefabName, out var dt)) {
                            mData.dt = dt;
                            musicDatas[i] = mData;
                        }
                    }
                }
                var showTick = mData.tick - mData.dt;
                delay = (showTick < delay) ? showTick : delay;
            }
            delay = Il2CppSystem.Decimal.Round(delay, 3);

            Log.Debug("Processed Delay");
        }
    
        /// <summary>
        /// Assigns gemini pairs.
        /// Also ensures everything else is not assigned a pair.
        /// </summary>
        private static void ProcessGeminis() {
            var geminiCache = new Dictionary<Il2CppSystem.Decimal, List<MusicData>>();

            for(int i = 1; i < musicDatas.Count; i++) {
                MusicData mData = musicDatas[i];
                mData.doubleIdx = -1;
                musicDatas[i] = mData;

                // Only Normal and Ghost notes get the gemini check
                if(mData.noteData.type != 1 && mData.noteData.type != 4) {
                    continue;
                }

                if(geminiCache.ContainsKey(mData.tick)) {
                    var gemList = geminiCache[mData.tick];
                    var mDataGemini = mData.noteData.ibms_id == "0E";
                    var targetGemini = false;
                    var target = new MusicData();

                    // Find an appropriate match among notes of the same tick
                    // Geminis should always match opposite geminis, even if overlapped with other note types
                    foreach(var gemData in gemList) {
                        if(mData.isAir == gemData.isAir) continue;

                        target = gemData;
                        targetGemini = gemData.noteData.ibms_id == "0E";

                        if(mDataGemini && gemData.noteData.ibms_id == "0E") break;
                        if(!mDataGemini) break;
                    }

                    if(target.objId > 0) {
                        mData.isDouble = mDataGemini && targetGemini;
                        mData.doubleIdx = target.objId;
                        target.isDouble = mDataGemini && targetGemini;
                        target.doubleIdx = mData.objId;

                        musicDatas[mData.objId] = mData;
                        musicDatas[target.objId] = target;
                    }
                } else {
                    geminiCache[mData.tick] = new List<MusicData>();
                }

                geminiCache[mData.tick].Add(mData);
            }

            Log.Debug("Processed Geminis");
        }

        /// <summary>
        /// Applies the chart-wide offset delay.
        /// Also sets showTick.
        /// </summary>
        private static void ApplyDelay() {
            for(int i = 0; i < musicDatas.Count; i++) {
                MusicData mData = musicDatas[i];
                mData.tick -= delay;
                mData.showTick = Il2CppSystem.Decimal.Round(mData.tick - mData.dt, 2);
                if(mData.isLongPressType) {
                    mData.endIndex -= (int)(delay / (Il2CppSystem.Decimal)0.001f);
                }
                musicDatas[i] = mData;
            }

            Log.Debug("Applied Delay");
        }
    
        /// <summary>
        /// Converts an Il2Cpp JSON object to a MusicConfigData object.
        /// Required as AOT code does not exist for the ToObject function.
        /// </summary>
        /// <param name="jObject"></param>
        /// <returns></returns>
        private static MusicConfigData JsonToConfigData(JObject jObject) {
            var config = new MusicConfigData();
            config.id = (int)jObject["id"];
            config.time = (Il2CppSystem.Decimal)(float)jObject["time"];
            config.note_uid = (string)jObject["note_uid"];
            config.length = (Il2CppSystem.Decimal)(float)jObject["length"];
            config.pathway = (int)jObject["pathway"];
            config.blood = (bool)jObject["blood"];

            return config;
        }
    }
}
