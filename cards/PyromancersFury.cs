// Scripted by 0x4261756D
using CardGameCore;
using static CardGameUtils.GameConstants;

class PyromancersFury : Spell
{
	private bool creatureDiedActive = false;
	public PyromancersFury() : base(
		Name: "Pyromancer's Fury",
		CardClass: PlayerClass.Pyromancer,
		OriginalCost: 1,
		Text: "{Cast}: Your \"Ignite\" deals +1 damage for the rest of the game. {A creature dies this turn}: Your ability is refreshed."
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new CastTrigger(effect: CastEffect), referrer: this);
		RegisterGenericDeathTrigger(trigger: new GenericDeathTrigger(effect: RefreshEffect, condition: (_) => creatureDiedActive, influenceLocation: Location.Grave), referrer: this);
	}

	public void RefreshEffect(Card _)
	{
		RefreshAbility(Controller);
	}

	public void CastEffect()
	{
		ChangeIgniteDamage(player: Controller, amount: 1);
		creatureDiedActive = true;
		RegisterStateReachedTrigger(trigger: new StateReachedTrigger(effect: ResetEffect, state: State.TurnEnd, influenceLocation: Location.ALL, oneshot: true), referrer: this);
	}

	public void ResetEffect()
	{
		creatureDiedActive = false;
	}
}