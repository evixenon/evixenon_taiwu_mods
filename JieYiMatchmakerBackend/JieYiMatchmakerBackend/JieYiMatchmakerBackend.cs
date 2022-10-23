using System;
using TaiwuModdingLib.Core.Plugin;
using HarmonyLib;
using GameData.Utilities;
using GameData.Domains.Character.Relation;
using GameData.Domains;

namespace JieYiMatchmakerBackend
{
    [PluginConfig("JieYiMatchmaker", "evixenon", "0.1.1")]
    public class JieYiMatchmakerBackend : TaiwuRemakePlugin
    {
        Harmony harmony;
        public override void Dispose()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }

        public override void Initialize()
        {
            harmony = Harmony.CreateAndPatchAll(typeof(JieYiMatchmakerBackend));
        }

        [HarmonyPrefix, HarmonyPatch(typeof(RelationType), "AllowAddingHusbandOrWifeRelation")]
        public static bool AllowAddingHusbandOrWifeRelationPrefix(int charId, int relatedCharId, ref bool __result)
        {
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
    }
}
