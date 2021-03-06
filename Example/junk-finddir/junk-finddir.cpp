// junk-finddir.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"


#include <iostream>
#include <experimental/filesystem>
#include <string>
#include <stdio.h>
#include <Shlwapi.h>
#include <set>

using namespace std;

set<string> extensions = { ".doc", ".docx", ".txt" }; //things to look for
set<string> ignoredirs = { "Windows", "Program Files" }; //and other ones that i was too lazy to write in there
using namespace std::experimental::filesystem;

int main()
{
	for (recursive_directory_iterator i("c:\\"), end; i != end; ++i)
	{
		if (!is_directory(i->path()) && i->path().has_extension()) // checks ifthe file even has an extension
		{
			if (extensions.find(i->path().extension().string()) != extensions.end())
				cout << "found document at :";
			cout << i->path().string() << endl; //print out the path

		}

		if (ignoredirs.find(i->path().filename().string()) != ignoredirs.end())
			i.disable_recursion_pending();
	}
}

//
//#include <iostream>
//#include <string>
//#include <experimental/filesystem>
//namespace fs = std::experimental::filesystem;
//
//int main()
//{
//	fs::create_directories("sandbox/a/b/c");
//	fs::create_directories("sandbox/a/b/d/e");
//	std::ofstream("sandbox/a/b/file1.txt");
//	fs::create_symlink("a", "sandbox/syma");
//	for (auto i = fs::recursive_directory_iterator("sandbox");
//		i != fs::recursive_directory_iterator();
//		++i) {
//		std::cout << std::string(i.depth(), ' ') << *i;
//		if (fs::is_symlink(i->symlink_status()))
//			std::cout << " -> " << fs::read_symlink(*i);
//		std::cout << '\n';
//
//		// do not descend into "b"
//		if (i->path().filename() == "b")
//			i.disable_recursion_pending();
//	}
//	fs::remove_all("sandbox");
//}