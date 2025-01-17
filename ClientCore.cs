using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CardGameUtils;
using CardGameUtils.Structs;
using static CardGameUtils.Functions;
using static CardGameUtils.Structs.NetworkingStructs;

namespace CardGameCore;

class ClientCore : Core
{
	readonly static List<CardGameUtils.Structs.CardStruct> cards = new List<CardGameUtils.Structs.CardStruct>();
	readonly static List<DeckPackets.Deck> decks = new List<DeckPackets.Deck>();
	public ClientCore()
	{
		if(Program.config.deck_config == null)
		{
			Functions.Log("Deck config was null when creating a client core", Functions.LogSeverity.Error);
			return;
		}
		if(!Directory.Exists(Program.config.deck_config.deck_location))
		{
			Log($"Deck folder not found, creating it at {Program.config.deck_config.deck_location}", LogSeverity.Warning);
			Directory.CreateDirectory(Program.config.deck_config.deck_location);
		}
		string[] deckfiles = Directory.GetFiles(Program.config.deck_config.deck_location);
		foreach(Type card in Assembly.GetExecutingAssembly().GetTypes().Where(Program.IsCardSubclass))
		{
			Card c = (Card)Activator.CreateInstance(card)!;
			cards.Add(c.ToStruct(client: true));
		}

		if(Program.config.deck_config.should_fetch_additional_cards)
		{
			TryFetchAdditionalCards();
		}

		foreach(string deckfile in deckfiles)
		{
			List<string> decklist = File.ReadAllLines(deckfile).ToList();
			DeckPackets.Deck deck = new CardGameUtils.Structs.NetworkingStructs.DeckPackets.Deck
			{
				player_class = Enum.Parse<GameConstants.PlayerClass>(decklist[0]),
				name = Path.GetFileNameWithoutExtension(deckfile)
			};
			decklist.RemoveAt(0);
			if(decklist[0].StartsWith("#"))
			{
				deck.ability = cards[cards.FindIndex(x => x.name == decklist[0].Substring(1))];
				decklist.RemoveAt(0);
			}
			if(decklist[0].StartsWith("|"))
			{
				deck.quest = cards[cards.FindIndex(x => x.name == decklist[0].Substring(1))];
				decklist.RemoveAt(0);
			}
			deck.cards = DecklistToCards(decklist);
			decks.Add(deck);
		}
	}

	public override void Init()
	{
		HandleNetworking();
		listener.Stop();
	}

	//TODO: This could be more elegant
	public static CardGameUtils.Structs.CardStruct[] DecklistToCards(List<string> decklist)
	{
		List<CardGameUtils.Structs.CardStruct> c = new List<CardGameUtils.Structs.CardStruct>();
		foreach(string line in decklist)
		{
			c.Add(cards[cards.FindIndex(x => x.name == line)]);
		}
		return c.ToArray();
	}
	public void TryFetchAdditionalCards()
	{
		try
		{
			using(TcpClient client = new TcpClient(Program.config.deck_config!.additional_cards_url.address, Program.config.deck_config.additional_cards_url.port))
			{
				using(NetworkStream stream = client.GetStream())
				{
					List<byte> payload = GeneratePayload<ServerPackets.AdditionalCardsRequest>(new ServerPackets.AdditionalCardsRequest());
					stream.Write(payload.ToArray(), 0, payload.Count);
					List<byte>? packet = ReceivePacket<ServerPackets.AdditionalCardsResponse>(stream, 3000);
					CardGameUtils.Structs.CardStruct[]? list = packet == null ? null : DeserializePayload<ServerPackets.AdditionalCardsResponse>(packet).cards;
					if(list == null)
					{
						return;
					}
					foreach(CardGameUtils.Structs.CardStruct card in list)
					{
						cards.Remove(card);
						cards.Add(card);
					}
				}
			}
		}
		catch(Exception e)
		{
			Log($"Could not fetch additional cards {e.Message}", severity: LogSeverity.Warning);
		}
	}
	public override void HandleNetworking()
	{
		listener.Start();
		List<byte> bytes = new List<byte>();
		while(true)
		{
			Log("Waiting for a connection");
			TcpClient client = listener.AcceptTcpClient();
			Log("Connected");
			using(NetworkStream stream = client.GetStream())
			{
				bytes = ReceiveRawPacket(stream)!;
				Log("Received a request");
				if(bytes.Count == 0)
				{
					Log("The request was empty, ignoring it", severity: LogSeverity.Warning);
				}
				else
				{
					if(HandlePacket(bytes, stream))
					{
						Log("Received a package that says the server should close");
						break;
					}
					Log("Sent a response");
				}
				stream.Close();
				client.Close();
			}
		}
		listener.Stop();
	}

	public bool HandlePacket(List<byte> bytes, NetworkStream stream)
	{
		// THIS MIGHT CHANGE AS SENDING RAW JSON MIGHT BE TOO EXPENSIVE/SLOW
		// possible improvements: Huffman or Burrows-Wheeler+RLE
		if(bytes[0] >= (byte)NetworkingConstants.PacketType.PACKET_COUNT)
		{
			throw new Exception($"ERROR: Unknown packet type encountered: ({bytes[0]})");
		}
		NetworkingConstants.PacketType type = (NetworkingConstants.PacketType)bytes[0];
		string packet = Encoding.UTF8.GetString(bytes.GetRange(1, bytes.Count - 1).ToArray());
		List<byte> payload = new List<byte>();
		switch(type)
		{
			case NetworkingConstants.PacketType.DeckNamesRequest:
			{
				DeckPackets.NamesRequest request = DeserializeJson<DeckPackets.NamesRequest>(packet);
				payload = GeneratePayload<DeckPackets.NamesResponse>(new DeckPackets.NamesResponse
				{
					names = decks.ConvertAll(x => x.name).ToArray()
				});
			}
			break;
			case NetworkingConstants.PacketType.DeckListRequest:
			{
				DeckPackets.ListRequest request = DeserializeJson<DeckPackets.ListRequest>(packet);
				payload = GeneratePayload<DeckPackets.ListResponse>(new DeckPackets.ListResponse
				{
					deck = FindDeckByName(request.name!),
				});
			}
			break;
			case NetworkingConstants.PacketType.DeckSearchRequest:
			{
				DeckPackets.SearchRequest request = DeserializeJson<DeckPackets.SearchRequest>(packet);
				payload = GeneratePayload<DeckPackets.SearchResponse>(new DeckPackets.SearchResponse
				{
					cards = FilterCards(cards, request.filter!, request.playerClass).ToArray()
				});
			}
			break;
			case NetworkingConstants.PacketType.DeckListUpdateRequest:
			{
				DeckPackets.Deck deck = DeserializeJson<DeckPackets.ListUpdateRequest>(packet).deck;
				deck.name = Regex.Replace(deck.name, @"[\./\\]", "");
				int index = decks.FindIndex(x => x.name == deck.name);
				if(deck.cards != null)
				{
					if(index == -1)
					{
						decks.Add(deck);
					}
					else
					{
						decks[index] = deck;
					}
					SaveDeck(deck);
				}
				else
				{
					if(index != -1)
					{
						decks.RemoveAt(index);
						File.Delete(Path.Combine(Program.config.deck_config!.deck_location, deck.name + ".dek"));
					}
				}
				payload = GeneratePayload<DeckPackets.ListUpdateResponse>(new DeckPackets.ListUpdateResponse { should_update = index == -1 });
			}
			break;
			default:
				throw new Exception($"ERROR: Unable to process this packet: ({type}) | {packet}");
		}
		stream.Write(payload.ToArray(), 0, payload.Count);
		return false;
	}

	private void SaveDeck(DeckPackets.Deck deck)
	{
		string? deckString = deck.ToString();
		if(deckString == null) return;
		File.WriteAllText(Path.Combine(Program.config.deck_config!.deck_location, deck.name + ".dek"), deckString);
	}

	private List<CardStruct> FilterCards(List<CardStruct> cards, string filter, GameConstants.PlayerClass playerClass)
	{
		return cards.Where(x =>
			(playerClass == GameConstants.PlayerClass.All || x.card_class == GameConstants.PlayerClass.All || x.card_class == playerClass)
			&& x.ToString().ToLower().Contains(filter)).ToList();
	}

	private DeckPackets.Deck FindDeckByName(string name)
	{
		name = Regex.Replace(name, @"[\./\\]", "");
		return decks[decks.FindIndex(x => x.name == name)];
	}
}