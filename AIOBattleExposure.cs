using Mono.Cecil;
using System;
using System.Linq;

namespace Goose.Monsterpatch.SocialPatcher
{
    internal static class AIOBattleExposure
    {
        public static int ExposeBattleSystemMethods(AssemblyDefinition assembly)
        {
            try
            {
                TypeDefinition battleSystem = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "BattleSystem");
                if (battleSystem == null)
                {
                    Console.WriteLine("[MMOnsterpatch AIO Patcher] BattleSystem not found; no battle methods exposed.");
                    return 0;
                }

                string[] expose =
                {
                    "ApplyRawDamageToTarget",
                    "ApplyDamageToTarget",
                    "ResolveMove",
                    "ResolveMoveEffectLoop",
                    "BuildBaseTargets",
                    "BuildRandomTargetsForThisTrigger",
                    "GetEnemySlotObject",
                    "GetPlayerSlotObject",
                    "CalculateDamage"
                };

                int changed = 0;
                foreach (MethodDefinition m in battleSystem.Methods)
                {
                    if (!expose.Contains(m.Name))
                        continue;

                    if (m.IsPrivate || m.IsFamily || m.IsAssembly)
                    {
                        m.IsPrivate = false;
                        m.IsFamily = false;
                        m.IsAssembly = false;
                        m.IsPublic = true;
                        changed++;
                    }
                }

                Console.WriteLine("[MMOnsterpatch AIO Patcher] Exposed " + changed + " BattleSystem methods for authoritative PvP hit replay.");
                return changed;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MMOnsterpatch AIO Patcher] Battle exposure failed: " + ex);
                return 0;
            }
        }
    }
}
