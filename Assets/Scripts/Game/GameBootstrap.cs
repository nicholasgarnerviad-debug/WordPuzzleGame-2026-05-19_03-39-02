using UnityEngine;
using System.Threading.Tasks;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private ModeController modeController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameplayScreen gameplayScreen;
    [SerializeField] private MainMenuScreen mainMenuScreen;
    [SerializeField] private ResultsScreen resultsScreen;

    private void Awake()
    {
        Debug.Log("[Bootstrap] Awake started");
        var dataManager = new DataManager();
        var wordGraph   = new WordGraph();

        // Seed word list — replace with Resources.Load<TextAsset>("wordlist") for full dictionary
        string[] words = {
            "cat","bat","hat","mat","map","cap","cup","cut","but","bus","bun","ban",
            "can","tan","tap","tip","tin","bin","bit","sit","six","mix","fix","fig",
            "fit","hit","hip","him","dim","dip","dig","big","bag","bad","bed","red",
            "rid","rig","rip","rim","ram","ran","rat","rot","rod","rob","rib","rub",
            "run","fun","sun","son","ton","top","mop","mob","bob","bog","dog","dot",
            "hot","hog","fog","fin","fan","van","vat","ham","jam","jar","tar","bar",
            "bay","day","say","sat","sad","mad","cab","cob","cod","cop","cot","cow",
            "den","dew","die","doe","don","dug","dun","ear","eat","egg","elf","elk",
            "end","fad","flu","fly","foe","fox","gag","gal","gap","gas","gel","gem",
            "get","gig","god","got","gum","gun","gut","guy","hag","has","hay","hen",
            "her","hew","hey","hid","his","hob","hoe","hop","how","hub","hue","hug",
            "hum","hut","ill","imp","ink","inn","ion","ivy","jab","jaw","jay","jet",
            "jig","job","jog","joy","jug","jut","keg","key","kid","kin","kit","lab",
            "lad","lag","lap","law","lax","lay","led","leg","let","lid","lip","lit",
            "lob","log","lot","low","lug","man","mar","may","mid","mod","mud","mug",
            "nag","nap","nit","nod","nor","not","nun","nut","oak","oaf","oar","oat",
            "odd","off","oil","old","orb","ore","our","out","owe","owl","own","pad",
            "pal","pan","par","pat","paw","pay","pea","peg","pen","per","pet","pie",
            "pig","pin","pit","pod","pop","pot","pub","pug","pun","pup","put","rag",
            "rap","raw","ray","row","rut","rye","sag","sap","saw","sea","set","sew",
            "sin","sip","sky","sob","sod","sow","sub","sue","sum","tab","tad","tag",
            "tea","ten","tie","toe","tog","tot","tow","toy","tub","tug","two","urn",
            "van","vow","wad","wag","web","wed","wig","win","wit","woe","won","woo",
            "yam","yap","yaw","yet","yew","you","yup","zap","zen","zip","zoo","ace",
            "age","ago","aid","aim","air","ale","ant","ape","arc","are","ark","arm",
            "art","ash","ask","ate","awe","axe","aye","bee","bog","bow","boy","bun",
            "bye","caw","cry","cub","cud","cue","dab","dam","dew","dye","eve","ewe",
            "eye","fawn","fern","fire","five","flat","flew","flit","flow","foam","fond",
            "ford","fore","fork","form","fort","four","fowl","free","fret","frog","from",
            "fuel","full","fume","fury","fuse","gain","gale","gall","game","gang","gave",
            "gaze","gear","gist","give","glad","glen","glib","glob","glue","glum","gnaw",
            "goal","goat","goes","gold","golf","gone","gore","gown","grab","gram","gray",
            "grew","grid","grim","grin","grip","grit","grow","grub","gulf","gull","gulp",
            "guru","hail","hair","hall","halt","hand","hang","hank","hard","hare","harm",
            "harp","hast","hate","haul","have","hawk","haze","head","heal","heap","hear",
            "heat","heel","held","hell","help","herd","hero","hewn","hide","high","hill",
            "hilt","hire","hoar","hoax","hold","hole","holy","home","hood","hoop","hope",
            "horn","hose","host","hour","hull","hunt","hurl","hurt","hymn","idea","idle",
            "into","iron","isle","itch","item","jade","jail","jest","join","joke","jolt",
            "junk","jury","just","keen","kelp","kept","kern","kill","kind","king","knit",
            "knob","knot","know","lace","lack","laid","lame","lamp","land","lane","lard",
            "lark","lash","lass","last","late","laud","lava","lawn","lead","leaf","lean",
            "leap","lend","lent","less","liar","lick","life","lift","like","lime","limp",
            "line","link","lion","list","live","load","loam","loan","lock","loft","lone",
            "long","look","loom","loon","loop","lore","lorn","lory","lose","lost","loud",
            "love","luck","lull","lump","lung","lure","lurk","lust","mace","made","maid",
            "mail","main","make","male","mall","malt","mane","many","mare","mark","mars",
            "mash","mask","mass","mast","mate","maul","maze","meal","mean","meat","melt",
            "memo","mere","mesh","mild","mile","mill","mime","mind","mine","mint","mire",
            "miss","mist","moan","moat","mock","mode","mold","mole","molt","monk","mood",
            "moon","moor","more","morn","most","moth","much","muck","mule","myth","nail",
            "name","nape","navy","near","neat","need","nest","news","next","nice","nick",
            "nine","node","noise","note","noun","nude","null","obey","obra","odor","once",
            "only","open","oral","orgy","oven","over","pace","pack","page","paid","pain",
            "pale","palm","pane","park","part","pass","past","path","pave","peak","peal",
            "pear","peel","peer","pelt","pest","pick","pied","pile","pine","pink","pipe",
            "plan","play","plot","plow","ploy","plum","plus","poem","poet","pole","poll",
            "pond","pone","pool","pore","port","pose","post","pour","pray","prep","prey",
            "prod","prop","pull","pump","pure","push","quit","quiz","race","rack","raft",
            "rage","raid","rail","rain","rake","ramp","rang","rank","rant","rash","rate",
            "rave","read","real","ream","reap","rein","rely","rend","rent","rest","rice",
            "rich","ride","rife","rift","ring","riot","rise","risk","roam","roar","robe",
            "rock","role","roll","roof","rook","room","rope","rose","ruin","rule","rush",
            "rust","safe","sage","sail","sake","sale","salt","same","sand","sane","sang",
            "sank","sash","save","scan","scar","seal","seam","sear","seat","seed","seek",
            "seem","seep","seer","self","sell","send","sent","shed","shin","ship","shop",
            "shot","show","shun","shut","side","sigh","silk","sill","silo","sing","sire",
            "size","skin","skip","slab","slam","slap","slat","slay","sled","slew","slim",
            "slip","slit","slop","slot","slow","slug","slum","slur","smug","snap","snip",
            "snob","snug","soak","soap","soar","sock","soil","sold","sole","some","song",
            "sort","soul","soup","sour","span","spar","spat","spec","sped","spin","spit",
            "spot","spry","stab","star","stem","step","stew","stir","stop","stow","stub",
            "stud","stun","suck","suit","sulk","sung","sunk","sure","swam","swan","swap",
            "swat","sway","swim","swum","tail","tale","talk","tall","tame","tamp","tank",
            "tape","task","taut","team","tear","teat","teem","tell","temp","tend","tent",
            "term","test","text","than","thaw","them","then","they","thin","this","thou",
            "tick","tide","time","tint","tire","toad","toga","told","toll","tomb","tone",
            "tool","tore","torn","toss","tour","town","trap","tray","trek","trim","trio",
            "trip","trod","trot","true","tuft","tune","turf","turn","tusk","tutu","twin",
            "type","typo","ugly","ulna","undo","unit","unto","upon","used","user","vary",
            "vast","veal","veer","veil","vein","verb","very","vest","vibe","vice","view",
            "vine","void","vote","wade","wage","wail","wait","wake","walk","wall","wand",
            "ward","warm","warn","wart","wave","wavy","weak","weal","wean","wear","weed",
            "week","weld","well","welt","went","were","west","what","when","whim","whip",
            "whir","whiz","wick","wide","wife","wild","will","wilt","wine","wing","wink",
            "wire","wise","wish","wolf","wood","wool","word","wore","work","worm","worn",
            "wrap","wren","writ","yell","yoga","yoke","yore","your","zero","zeal","zest"
        };
        foreach (var w in words) wordGraph.AddWord(w);
        wordGraph.BuildAdjacencies();

        var wordValidator   = new WordValidator(wordGraph);
        var puzzleGenerator = new PuzzleGenerator(wordGraph);
        var economyManager  = new EconomyManager(dataManager);
        var stateManager    = new GameStateManager(wordValidator, dataManager);

        Debug.Log("[Bootstrap] Starting async service initialization");
        _ = InitServicesAsync(economyManager, dataManager);

        Debug.Log("[Bootstrap] Initializing mode controller");
        modeController.Initialize(dataManager, economyManager, puzzleGenerator,
                                  stateManager, wordValidator);
        Debug.Log("[Bootstrap] Mode controller initialized");

        Debug.Log("[Bootstrap] Registering UI screens");
        uiManager.RegisterScreen(mainMenuScreen);
        uiManager.RegisterScreen(gameplayScreen);
        uiManager.RegisterScreen(resultsScreen);

        Debug.Log("[Bootstrap] Injecting dependencies into screens");
        mainMenuScreen.InjectDependencies(modeController, uiManager);
        gameplayScreen.InjectDependencies(stateManager, modeController, uiManager);
        resultsScreen.InjectDependencies(modeController, uiManager);

        modeController.ModeCompleted += stats =>
        {
            var finalStats = stateManager.GetFinalStats();
            resultsScreen.ShowResults(finalStats);
            uiManager.ShowScreen<ResultsScreen>();
        };

        Debug.Log("[Bootstrap] Showing MainMenuScreen");
        uiManager.ShowScreen<MainMenuScreen>();
        Debug.Log("[Bootstrap] Awake complete");
    }

    private async Task InitServicesAsync(EconomyManager economy, DataManager data)
    {
        await economy.InitializeAsync();
        await data.LoadAllTierDataAsync();
    }
}
