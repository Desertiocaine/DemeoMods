namespace RulesAPI
{
    using System;
    using System.Linq;
    using MelonLoader;

    public static class RulesAPI
    {
        internal static readonly MelonLogger.Instance Logger = new MelonLogger.Instance("RulesAPI");

        internal static Ruleset SelectedRuleset { get; private set; }

        public static void SelectRuleset(string ruleset)
        {
            try
            {
                SelectedRuleset = Registrar.Instance().Rulesets
                    .Single(r => string.Equals(r.Name, ruleset, StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException e)
            {
                throw new ArgumentException("Ruleset must first be registered.", e);
            }

            Logger.Msg($"Selected ruleset: {SelectedRuleset.Name}");
        }

        internal static void ActivateSelectedRuleset()
        {
            if (SelectedRuleset == null)
            {
                return;
            }

            SelectedRuleset.Activate();
        }

        internal static void DeactivateSelectedRuleset()
        {
            SelectedRuleset.Deactivate();
        }

        internal static void TriggerPreGameCreated()
        {
            foreach (var rule in SelectedRuleset.Rules)
            {
                try
                {
                    rule.PreGameCreated();
                }
                catch (Exception e)
                {
                    // TODO(orendain): Rollback activation.
                    RulesAPI.Logger.Warning($"Failed to successfully call PreGameCreated on rule [{rule.GetType()}]: {e}");
                }
            }
        }

        internal static void TriggerPostGameCreated()
        {
            foreach (var rule in SelectedRuleset.Rules)
            {
                try
                {
                    rule.PostGameCreated();
                }
                catch (Exception e)
                {
                    // TODO(orendain): Rollback activation.
                    RulesAPI.Logger.Warning($"Failed to successfully call PostGameCreated on rule [{rule.GetType()}]: {e}");
                }
            }
        }
    }
}
