using WebAppSandbox.GameEngine.Models;

namespace WebAppSandbox.GameEngine.Cards;

public static class CardLibrary
{
    public const int AttacksPerOrgan = 2;
    public const int TreatmentCopies = 4;
    public const int DefenseCopies = 4;
    public const int NecrosisCopies = 5;
    public const int TransplantCopies = 1;

    /// <summary>
    /// Builds the card library for a specific set of organ types.
    /// Specific afflictions from rules.md are added for each organ that is in play.
    /// Multi-target afflictions are added once when at least one of their targets is in play.
    /// The Wild organ can only be destroyed by accumulating 4 afflictions, so no standard
    /// attack cards are added for it.
    /// </summary>
    public static IReadOnlyList<Card> BuildDeck(IReadOnlyCollection<OrganType> organTypes)
    {
        var cards = new List<Card>();

        // Specific & general affliction cards (from rules.md).
        foreach (var affliction in SpecificAfflictions)
        {
            var validTargets = affliction.TargetOrganTypes is { Length: > 0 }
                ? affliction.TargetOrganTypes
                : new[] { affliction.TargetOrganType!.Value };

            if (!validTargets.Any(organTypes.Contains))
            {
                continue;
            }

            for (int copy = 0; copy < affliction.Copies; copy++)
            {
                cards.Add(new Card
                {
                    Id = $"affliction-{Slugify(affliction.Name)}-{copy}",
                    Name = affliction.Name,
                    Type = CardType.Affliction,
                    TargetSide = TargetSide.Opponent,
                    TargetOrganType = affliction.TargetOrganType,
                    TargetOrganTypes = affliction.TargetOrganTypes ?? Array.Empty<OrganType>(),
                    AfflictionType = AfflictionType.Afflicted,
                    AfflictionAmount = 1,
                    Description = affliction.Description,
                });
            }
        }

        // Standard attacks per organ (not for the Wild organ).
        foreach (var organ in organTypes)
        {
            if (organ == OrganType.Wild_Organ)
            {
                continue;
            }

            for (int i = 0; i < AttacksPerOrgan; i++)
            {
                cards.Add(new Card
                {
                    Id = $"attack-{organ.ToString().ToLowerInvariant()}-{i}",
                    Name = $"Attack {organ}",
                    Type = CardType.Attack,
                    TargetSide = TargetSide.Opponent,
                    TargetOrganType = organ,
                    Description = $"Destroy target's afflicted {organ}."
                });
            }
        }

        // Necrosis: counts as 2 full afflictions on any organ.
        for (int i = 0; i < NecrosisCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"necrosis-{i}",
                Name = "Necrosis",
                Type = CardType.Attack,
                TargetSide = TargetSide.Opponent,
                AfflictionAmount = 2,
                Description = "Counts as 2 full afflictions on any organ."
            });
        }

        // Transplant: steal one completely healthy organ from an opponent.
        for (int i = 0; i < TransplantCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"transplant-{i}",
                Name = "Transplant",
                Type = CardType.Special,
                TargetSide = TargetSide.Opponent,
                SpecialCard = SpecialCardType.Transplant,
                Description = "Permanently steal one completely healthy organ from an opponent's body to add to your own."
            });
        }

        // Treatment: 4 generic (target self, remove affliction)
        for (int i = 0; i < TreatmentCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"treatment-{i}",
                Name = "Treatment",
                Type = CardType.Treatment,
                TargetSide = TargetSide.Self,
                Description = "Remove all afflictions from one of your organs."
            });
        }

        // Defense: 4 generic (target self, shield)
        for (int i = 0; i < DefenseCopies; i++)
        {
            cards.Add(new Card
            {
                Id = $"defense-{i}",
                Name = "Defense",
                Type = CardType.Defense,
                TargetSide = TargetSide.Self,
                Description = "Shield one of your organs."
            });
        }

        return cards;
    }

    private static string Slugify(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char c in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sb.Append(c);
            }
            else if (c == ' ')
            {
                sb.Append('-');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Every specific & general affliction card from rules.md, minus cards whose
    /// effects cannot be represented in the MVP (such as Infection!, whose target
    /// depends on a variable bacterial text line). Descriptions are reproduced
    /// verbatim from rules.md.
    /// </summary>
    private static readonly IReadOnlyList<SpecificAffliction> SpecificAfflictions = new List<SpecificAffliction>
    {
        // Appendix
        new("Appendicitis!", "Usus buntu membengkak, rasanya kayak ada bom waktu di perut bawah kanan.", OrganType.Appendix),
        new("Ruptured Appendix!", "Usus buntu meledak dan menyebarkan bakteri jahat ke seluruh area perut.", OrganType.Appendix),

        // Bladder
        new("Overactive Bladder!", "Kandung kemih terlalu sensitif, baru minum sedikit bawaannya mau beser terus.", OrganType.Bladder),
        new("UTI!", "Bakteri jahat nongkrong di saluran kencing, bikin sensasi buang air kecil rasanya perih menari-nari.", OrganType.Bladder),

        // Bones
        new("Fracture!", "Tulang kamu retak atau patah gara-gara sok-sokan jadi atraksi stuntman.", OrganType.Bones),
        new("Osteoporosis", "Tulang kamu pengeroposan, keroposnya bikin gampang retak kayak kerupuk.", OrganType.Bones),

        // Bowels
        new("Crohn's!", "Saluran pencernaan peradangan hebat, bikin kamu bolak-balik ke toilet kayak latihan maraton.", OrganType.Bowels),
        new("Day-Old Burrito!", "Makanan kemarin malam yang nekat dimakan, berujung bencana dahsyat di dalam perut.", OrganType.Bowels),
        new("IBS-d!", "Perut mules mendadak yang bikin kamu ahli memetakan posisi toilet umum terdekat.", OrganType.Bowels),

        // Brain
        new("Multiple Sclerosis!", "Kabel saraf di otak korsleting, bikin sinyal dari otak ke tubuh sering lag.", OrganType.Brain),
        new("Narcolepsy!", "Otak kamu mendadak nge-brik dan bikin ketiduran di mana saja tanpa permisi.", OrganType.Brain, Copies: 2),
        new("Stroke!", "Pasokan darah ke otak mendadak terputus, bikin sistem pusat tubuh mendadak down.", OrganType.Brain),

        // Esophagus
        new("Acid Reflux!", "Asam lambung naik bikin tenggorokan kerasa kayak kejatuhan lava panas.", OrganType.Esophagus),
        new("Esophageal Stricture!", "Kerongkongan menyempit, bikin makanan berasa kayak kejebak macet sebelum nyampe lambung.", OrganType.Esophagus),
        new("Heartburn!", "Dada kerasa terbakar hebat gara-gara asam lambung salah jalan naik ke atas.", OrganType.Esophagus),

        // Eyes
        new("Conjunctivitis!", "Mata belekan dan merah membara, bikin kamu kelihatan kayak mata-mata alien.", OrganType.Eyes),
        new("Foreign Object in the Eye!", "Debu sekecil debu atom masuk mata, tapi sakitnya serasa kemasukan kelapa.", OrganType.Eyes),
        new("Glaucoma!", "Tekanan di bola mata meningkat, bikin pandangan menyempit kayak ngintip lewat lubang kunci.", OrganType.Eyes),

        // Gallbladder
        new("Biliary Dyskinesia!", "Kantung empedu kamu mendadak malas kerja dan menolak buat meremas asam empedu.", OrganType.Gallbladder),
        new("Gallstones!", "Cairan empedu mengeras jadi kerikil padat yang nyangkut di saluran pencernaan.", OrganType.Gallbladder),

        // Heart
        new("Arrhythmia!", "Jantung kamu ketukannya gak beraturan, serasa lagi nyetel musik heavy metal di dalam dada.", OrganType.Heart),
        new("Heart Attack!", "Jantung mendadak kena shock karena aliran darah kebalap sumbatan lemak.", OrganType.Heart),

        // Kidneys
        new("Calcium Stones!", "Ginjalmu iseng bikin koleksi batu akik dari endapan kalsium yang rasanya mules bukan main.", OrganType.Kidneys),

        // Liver
        new("Cirrhosis!", "Hati kamu kapalan dan mengeras karena capek kebanyakan kerja keras memproses racun.", OrganType.Liver),
        new("Fatty Liver!", "Hati kamu ketimbun lemak, penanda kurang gerak dan kebanyakan makan yang enak-enak.", OrganType.Liver),

        // Lungs
        new("Asthma!", "Sensasi bernapas kayak pake sedotan es teh yang kejepit: bengek dan ngos-ngosan.", OrganType.Lungs),
        new("Cystic Fibrosis!", "Lendir tubuh mendadak tebal dan lengket, sampai organ pernapasan kerasa penuh lem.", OrganType.Lungs),

        // Muscles
        new("Muscle Contusion!", "Otot kamu kebentur keras sampai lebam kebiruan dan pegal-pegal.", OrganType.Muscles),
        new("Muscular Dystrophy!", "Otot-otot tubuh pelan-pelan melemah dan kehilangan kekuatannya untuk beraktivitas.", OrganType.Muscles),

        // Nose
        new("Common Cold!", "Penyakit langganan sejuta umat yang bikin kamu jadi pabrik ingus berjalan.", OrganType.Nose),
        new("Congestion!", "Hidung tersumbat sebelah, dijamin bikin kamu menyesal karena pernah meremehkan indahnya bernapas lega.", OrganType.Nose),
        new("Nosebleed!", "Pembuluh darah di hidung pecah, bikin kamu ngeluarin darah kayak adegan drama anime.", OrganType.Nose),

        // Pancreas
        new("Diabetes! Type 1", "Pankreas kamu mogok kerja dan berhenti produksi insulin, jadinya gula darah naik kelas.", OrganType.Pancreas),

        // Skin
        new("Acne!", "Bisul kecil di wajah yang selalu muncul pas kamu mau foto album atau first date.", OrganType.Skin),
        new("Psoriasis!", "Kulit beregenerasi kelewat cepat, bikin bersisik dan gatal tiada tara.", OrganType.Skin),
        new("Shingles!", "Cacar ular yang bikin kulit muncul ruam melepuh dan rasanya panas kayak disengat lebah.", OrganType.Skin),

        // Spleen
        new("Hypersplenism!", "Limpa kamu terlalu hiperaktif sampai-sampai sel darah yang sehat pun ikut disikat.", OrganType.Spleen),
        new("Lacerated Spleen!", "Limpa kamu robek kena hantaman keras, bikin pendarahan di dalam perut.", OrganType.Spleen),

        // Stomach
        new("Ulcer!", "Dinding lambung kamu lecet dan borokan kena kikis asam lambung sendiri.", OrganType.Stomach),

        // Teeth
        new("Cavity!", "Kamu malas sikat gigi, akhirnya bakteri bikin tempat nongkrong permanen di gigimu.", OrganType.Teeth),
        new("Enamel Erosion!", "Lapisan pelindung gigi terkikis habis gara-gara hobi minum yang asam-asam.", OrganType.Teeth),

        // Thyroid
        new("Hypothyroidism!", "Kelenjar tiroid kamu mager parah, bikin metabolisme tubuh berjalan kayak siput.", OrganType.Thyroid, Copies: 2),

        // Tongue
        new("Inflamed Taste Bud", "Lidah kamu bengkak dan perih gara-gara keseringan kegigit sendiri pas makan.", OrganType.Tongue),

        // Tonsils
        new("Chronic Strep Throat!", "Tenggorokan rasanya kayak diamplas tiap kali nelan ludah akibat bakteri membandel.", OrganType.Tonsils),
        new("Tonsillitis!", "Amandel kamu membengkak merah dan siap memblokir saluran tenggorokan.", OrganType.Tonsils),

        // Trachea
        new("Tracheitis!", "Batang tenggorokan kena infeksi, bikin batuk kamu bunyinya nyaring dan menyiksa.", OrganType.Trachea),

        // Multi-target afflictions
        new("Hepatosplenomegaly!", "Hati dan limpa kamu kompak membengkak barengan karena ada infeksi berat.",
            TargetOrganTypes: new[] { OrganType.Liver, OrganType.Spleen }),
        new("Walking Pneumonia", "Paru-paru kamu kena infeksi ringan, masih bisa jalan-jalan tapi sambil batuk serak tanpa henti.",
            TargetOrganTypes: new[] { OrganType.Lungs, OrganType.Pancreas }),
        new("Vomit", "Kamu muntah-muntah hebat sampai tenggorokan perih dan asam melintasi mulut.",
            TargetOrganTypes: new[] { OrganType.Stomach, OrganType.Esophagus, OrganType.Tongue, OrganType.Teeth }),
    };

    private sealed record SpecificAffliction(
        string Name,
        string Description,
        OrganType? TargetOrganType = null,
        OrganType[]? TargetOrganTypes = null,
        int Copies = 1);
}