using CardGameUtils;

namespace CardGameCore;

class Grave
{
	private List<Card> cards = new List<Card>();
	public int Size
	{
		get => cards.Count;
	}

	public Grave()
	{

	}

	internal void Add(Card card)
	{
		if(card.Keywords.ContainsKey(Keyword.Token) || card.Location == GameConstants.Location.UNKNOWN)
		{
			return;
		}
		card.Location = GameConstants.Location.Grave;
		cards.Add(card);
	}

	internal Card[] GetAll()
	{
		return cards.ToArray();
	}

	internal void Remove(Card card)
	{
		cards.Remove(card);
		card.Location &= ~GameConstants.Location.Grave;
	}
}