using Il2CppNewtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static Assets.Scripts.GameCore.Managers.iBMSCManager;

namespace CustomAlbums
{
	public static class BMSCLoader
	{
		/// <summary>
		/// A bms loader copied from MuseDash.
		/// 
		/// Ref: Assets.Scripts.GameCore.Managers.iBMSCManager
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="bmsName"></param>
		/// <returns></returns>
		public static BMS Load(Stream stream, string bmsName) {
			Dictionary<string, float> bpmTones = new Dictionary<string, float>();
			Dictionary<int, JToken> notesPercentDict = new Dictionary<int, JToken>();

			JObject info = new JObject();
			JArray notes = new JArray();

			JArray notesPercent = new JArray();
			List<JObject> list = new List<JObject>();

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
							JObject jObject = new JObject();
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
							JObject jObject = new JObject();
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
									JObject jObject = new JObject();
									jObject["tick"] = theTick;
									jObject["freq"] = freq;
									list.Add(jObject);
									list.Sort(delegate (JObject l, JObject r) {
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
									List<JObject> list2 = list.FindAll((JObject b) => (float)b["tick"] < theTick);
									for(int k = list2.Count - 1; k >= 0; k--) {
										JObject jobject5 = list2[k];
										float num5 = 0f;
										float num6 = (float)jobject5["freq"];
										if(k - 1 >= 0) {
											JObject jobject6 = list2[k - 1];
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
									JObject jobject7 = new JObject();
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

			notes._values.Sort((Il2CppSystem.Comparison<JToken>)((l, r) => {
				var lTime = (double)((float)l["time"]);
				var rTime = (double)((float)r["time"]);
				var lTone = (string)l["tone"];
				var rTone = (string)r["tone"];

				// This should be accurate for note sorting up to 6 decimal places
				var lScore = ((long)(lTime * 1_000_000) * 10) + (lTone == "15" ? 0 : 1);
				var rScore = ((long)(rTime * 1_000_000) * 10) + (rTone == "15" ? 0 : 1);

				return Math.Sign(lScore - rScore);
			}));

			BMS bms = new BMS {
				info = info,
				notes = notes,
				notesPercent = notesPercent,
				md5 = md5
			};
			bms.info["NAME"] = bmsName;
			bms.info["NEW"] = true;
			
			if(Il2CppSystem.Linq.Enumerable.ToList(bms.info.Properties()).Find((Il2CppSystem.Predicate<JProperty>)((JProperty p) => p.Name == "BANNER")) == null) {
				bms.info["BANNER"] = "cover/none_cover.png";
			} else {
				bms.info["BANNER"] = "cover/" + (string)bms.info["BANNER"];
			}
			return bms;
		}
	}
}
