#include <vector>
#include <string>
#include <map>


struct Item
{
    int width = 3;
    int length = 4;
};


class Collection
{
public:
    void Fill()
    {
        vec.push_back(new Item);
        vec.push_back(new Item{ 7,9 });
        vec.push_back(new Item{ 12, 16 });
    }

    // NOT EXPECTED! - Can modify item
    const std::vector<Item*>& GetMembers()
    {
        return vec;
    }

    // EXPECTED - Can't modify item, but slow
    std::vector<const Item*> GetMembersCCC() const
    {
        return std::vector<const Item*>(vec.begin(), vec.end());
    }

protected:
    std::vector<Item*> vec;
};


class MapCollection
{
public:
    void Fill()
    {
        map.insert({ "a", new Item });
        map.insert({ "b", new Item{ 7,9 } });
        map.insert({ "c", new Item{ 12, 16 } });
    }

    // HALF EXPECTED
    // This is actually a little correct because std::map::oprator[] isn't const, which is suprising!
    // However value can be modified by the .at() method, which is not expected!
    const std::map<std::string, Item*>& GetMembers()
    {
        return map;
    }

    // EXPECTED - Can't modify item, but slow
    std::map<std::string, const Item*> GetMembersCCC() const
    {
        return std::map<std::string, const Item*>(map.begin(), map.end());
    }

    // EXPECTED - Can't modify item, but slow
    std::vector<const Item*> GetMembersAsVector() const
    {
        std::vector<const Item*> vec;
        vec.reserve(map.size());
        for (const auto& pair : map)
            vec.push_back(pair.second);

        return vec;
    }

protected:
    std::map<std::string, Item*> map;
};


int main()
{
    // Verify vector Collection
    //
    Collection col;
    col.Fill();
    const std::vector<Item*>& members = col.GetMembers();
    members;
    Item* pItem = members[0];
    pItem->width = 0; // NOT EXPECTED!- Can modify item

    std::vector<const Item*> membersCCC = col.GetMembersCCC();
    membersCCC;
    const Item* pItemCCC = membersCCC[0];
    //pItemCCC->width = 0; // EXPECTED - Can't modify item
    pItemCCC;


    // Verify map Collection
    //
    MapCollection mapcol;
    mapcol.Fill();
    const std::map<std::string, Item*>& mapmembers = mapcol.GetMembers();
    mapmembers;
    //const Item* pMapItem = mapmembers["a"]; // EXPECTED - Can't modify item as std::map::oprator[] in map isn't const
    //pMapItem->width = 0;
    Item* pMapItem = mapmembers.at("a"); // NOT EXPECTED! - Can modify item
    pMapItem->width = 0; // NOT EXPECTED! - Can modify item

    std::map<std::string, const Item*> mapmembersCCC = mapcol.GetMembersCCC();
    mapmembersCCC;
    const Item* pMapItemCCC = mapmembersCCC["a"];
    //pMapItemCCC->width = 0; // EXPECTED - Can't modify item
    pMapItemCCC;

    std::vector<const Item*> mapToVec = mapcol.GetMembersAsVector();
    const Item* pMapToVecItem = mapToVec[0];
    //pMapToVecItem->width = 0; // EXPECTED - Can't modify item
    pMapToVecItem;

    return 0;
}
