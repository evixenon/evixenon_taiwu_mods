using System;
using System.Collections.Generic;

using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Utilities;
using GameData.Domains.Character.Relation;
using GameData.Domains;

using GameData.Domains.TaiwuEvent.EventHelper;
using GameData.Domains.Character;
using GameData.Domains.TaiwuEvent;
using GameData.Domains.TaiwuEvent.DisplayEvent;


namespace JieYiMatchmakerBackend
{
    [PluginConfig("JieYiMatchmaker", "evixenon", "0.2.0")]
    public class JieYiMatchmakerBackend : TaiwuRemakePlugin
    {
        Harmony harmony;

		public static bool jieYiFlag;
		public static bool sameSexFlag;

        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }

        public override void Initialize()
        {
			AdaptableLog.Info("[ModIdStr]:" + base.ModIdStr);

			// 从自身类寻找 Harmony 函数并 patch
            harmony = Harmony.CreateAndPatchAll(typeof(JieYiMatchmakerBackend));
        }

        public override void OnModSettingUpdate()
        {
            DomainManager.Mod.GetSetting(base.ModIdStr, "JieYiMarriagePossible", ref jieYiFlag);
            DomainManager.Mod.GetSetting(base.ModIdStr, "SameSexMarriagePossible", ref sameSexFlag);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RelationType), "AllowAddingHusbandOrWifeRelation")]
        public static bool AllowAddingHusbandOrWifeRelationPrefix(int charId, int relatedCharId, ref bool __result)
        {
			if (!jieYiFlag)
            {
				return true;
            }

			bool flag = DomainManager.Character.GetAliveSpouse(charId) >= 0 || DomainManager.Character.GetAliveSpouse(relatedCharId) >= 0;
			bool result;
			String participant = String.Concat(DomainManager.Character.GetRealName(charId));
			String relatedChar = String.Concat(DomainManager.Character.GetRealName(relatedCharId));
			AdaptableLog.Info(String.Format("尝试匹配{0}与{1}...", participant, relatedCharId));
			if (flag)
			{
				result = false;
			}
			else
			{
				RelatedCharacter relation;
				bool flag2 = !DomainManager.Character.TryGetRelation(charId, relatedCharId, out relation);
				if (flag2)
				{
					relation.RelationType = ushort.MaxValue;
				}
				bool flag3 = DomainManager.Character.HasNominalBloodRelation(charId, relatedCharId, relation);
				if (flag3)
				{
					result = false;
				}
				else
				{
					bool flag4 = relation.RelationType == ushort.MaxValue;
					if (flag4)
					{
						result = true;
					}
					else
					{
						ushort relationTypes = relation.RelationType;
						bool flag5 = relationTypes == 0;
						if (flag5)
						{
							result = true;
						}
						else
						{
							bool flag6 = (relationTypes & 1024) > 0;
							if (flag6)
							{
								result = false;
							}
							else
							{
								// 去掉血缘关系以外关系的判定
								// bool flag7 = RelationType.ContainBloodExclusionRelations(relationTypes);
								// result = !flag7;
								result = true;
							}
						}
					}
				}
			}
			__result = result;
			String res;
			if (__result)
            {
				res = "成功";
            } 
			else
            {
				res = "失败";
            }
			AdaptableLog.Info(res);
			return false;  // 不执行原函数
		}

        [HarmonyPrefix, HarmonyPatch(typeof(EventHelper), "SelectTaiwuTeammateForMatchmaking")]
		public static bool SelectTaiwuTeammateForMatchmakingPrefix(EventArgBox argBox, string saveKey, int charId)
		{
			if (!sameSexFlag)
            {
				return true;
            }

			Character targetCharacter;
			bool flag = !DomainManager.Character.TryGetElement_Objects(charId, out targetCharacter);
			if (flag)
			{
				throw new Exception(string.Format("can not find character of id {0} to SelectTaiwuTeammateForMatchmaking", charId));
			}
			EventSelectCharacterData data = new EventSelectCharacterData();
			data.FilterList = new List<CharacterSelectFilter>();
			CharacterSelectFilter filter = default(CharacterSelectFilter);
			filter.SelectKey = saveKey;
			filter.FilterTemplateId = -1;
			filter.AvailableCharacters = default(CharacterSet);
			int taiwuCharId = DomainManager.Taiwu.GetTaiwuCharId();
			HashSet<int> charHateCharIds = DomainManager.Character.GetRelatedCharIds(charId, 32768);

			foreach (int cellId in DomainManager.Taiwu.GetGroupCharIds().GetCollection())
			{
				bool flag2 = cellId == charId || charHateCharIds.Contains(cellId);
				if (!flag2)
				{
					Character character;
					bool flag3 = DomainManager.Character.TryGetElement_Objects(cellId, out character);
					if (flag3)
					{
						bool flag4 = character.GetCreatingType() == 0;
						if (!flag4)
						{
							bool flag5 = character.GetAgeGroup() != 2;
							if (!flag5)
							{
								bool flag6 = character.IsCompletelyInfected();
								if (!flag6)
								{
									sbyte favorLevel = FavorabilityType.GetFavorabilityType(DomainManager.Character.GetFavorability(cellId, taiwuCharId));
									bool flag7 = favorLevel < 4;
									if (!flag7)
									{
										// 允许同性
										// bool flag8 = !EventHelper.IsOppositeGender(character, targetCharacter);
										bool flag8 = false;
										if (!flag8)
										{
											bool flag9 = RelationType.AllowAddingHusbandOrWifeRelation(cellId, charId);
											if (flag9)
											{
												filter.AvailableCharacters.Add(cellId);
											}
										}
									}
								}
							}
						}
					}
				}
			}
			data.FilterList.Add(filter);
			argBox.Set("SelectCharacterData", data);
			return false;
		}

	}
}
