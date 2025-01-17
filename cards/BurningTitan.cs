// Scripted by 0x4261756D
using CardGameCore;
using static CardGameUtils.GameConstants;

class BurningTitan : Creature
{
	public BurningTitan() : base(
		Name: "Burning Titan",
		CardClass: PlayerClass.Pyromancer,
		OriginalCost: 6,
		Text: "[Collossal] +1\n Costs 1 less for each damage you dealt with spells last turn.\n{Revelation}: Gain 3 life.",
		OriginalPower: 7,
		OriginalLife: 7
		)
	{ }

	public override void Init()
	{
		RegisterKeyword(Keyword.Colossal, 1);
		RegisterLingeringEffect(info: new LingeringEffectInfo(effect: CostReductionEffect, referrer: this, influenceLocation: Location.Hand));
		RegisterRevelationTrigger(trigger: new RevelationTrigger(effect: GainEffect), referrer: this);
	}

	private void GainEffect()
	{
		PlayerChangeLife(player: Controller, amount: 3, source: this);
	}
	private void CostReductionEffect(Card _)
	{
		if(GetTurn() > 0)
		{
			this.Cost -= GetSpellDamageDealtXTurnsAgo(player: Controller, turns: 1);
		}
	}

}