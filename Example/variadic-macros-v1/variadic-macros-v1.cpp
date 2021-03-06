// variadic-macros-v1.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <stdio.h> 


using namespace std;

//#define QQQ1(AA) \
//	std::cout << #AA << endl;
//
//#define QQQ2(AA, BB) \
//	cout << #AA << " " << #BB << endl;
//
//#define QQQ3(AA, BB, CC) \
//	cout << #AA << " " << #BB << " " << #CC << endl;
//
//#define VVV(PP, ...) QQQ3(PP, __VA_ARGS__)
//
//
//int main()
//{
//	QQQ1(dog);
//	QQQ2(cat, mouse);
//
//	VVV("horse", "bird", "owl");
//
//	char junk;
//	cin >> junk;
//
//    return 0;
//}


#define EMPTY  

#define CHECK1(x, ...) if (!(x)) { printf(__VA_ARGS__); }  
#define CHECK2(x, ...) if ((x)) { printf(__VA_ARGS__); }  
#define CHECK3(...) { printf(__VA_ARGS__); }  
#define MACRO(s, ...) printf(s, __VA_ARGS__)  

//	static_assert("a" == "a" __VA_ARGS__, "BBBBBBBB");

//if (NN == eee)
//{
//	if ("a" == "a" __VA_ARGS__)
//		assert("zle");
//}

//#define AAA(NN, ...) \
//	static_assert((NN != eee) || ("a" != "a" __VA_ARGS__), "NNNNNNNNNN");

#define HASH_LIT #
#define HASH() HASH_LIT

//#define AAA(HASH_L, NN, ...) \
//	#pragma warning(suppress: 4130)\
//	static_assert((NN != eee) || ("" != "" __VA_ARGS__), "NNNNNNNNNN");

//warning C4130 : '!=' : logical operation on address of string constant


#define COUNT_IMPL(A, B, C, D, N, ...) N
#define COUNT(...) COUNT_IMPL(__VA_ARGS__, 4, 3, 2, 1)


//#define _GET_NTH_ARG(_1, _2, _3, _4, N, ...) N
//#define COUNT_VARARGS(...) _GET_NTH_ARG(__VA_ARGS__, 4, 3, 2, 1)


#define _GET_NTH_ARG(_1, _2, _3, _4, _5, N, ...) N
#define COUNT_VARARGS(...) _GET_NTH_ARG("ignored", ##__VA_ARGS__, 4, 3, 2, 1, 0)

//int main() {
//	printf("one arg: %d\n", COUNT_VARARGS(1));
//	printf("three args: %d\n", COUNT_VARARGS(1, 2, 3));
//}



//int main() {
//	CHECK1(0, "here %s %s %s", "are", "some", "varargs1(1)\n");
//	CHECK1(1, "here %s %s %s", "are", "some", "varargs1(2)\n");   // won't print  
//
//	CHECK2(0, "here %s %s %s", "are", "some", "varargs2(3)\n");   // won't print  
//	CHECK2(1, "here %s %s %s", "are", "some", "varargs2(4)\n");
//
//	// always invokes printf in the macro  
//	CHECK3("here %s %s %s", "are", "some", "varargs3(5)\n");
//
//	MACRO("hello, world\n");
//
//	MACRO("error\n", EMPTY); // would cause error C2059, except VC++   
//							 // suppresses the trailing comma  
//
//	constexpr int eee = 40; \
//
//	//AAA(HASH_LIT, 12);
//	//AAA(HASH_LIT, 67, "sdf");
//
//	//AAA(40, "sdf");
//	//AAA(40);
//
//	cout << endl;
//	cout << endl;
//	cout << COUNT(a, b) << endl;
//	cout << COUNT(a, b, c, d) << endl;
//
//}


template < unsigned N > constexpr
unsigned countarg(const char(&s)[N], unsigned i = 0, unsigned c = 0)
{
	return
		s[i] == '\0'
		? i == 0
		? 0
		: c + 1
		: s[i] == ','
		? countarg(s, i + 1, c + 1)
		: countarg(s, i + 1, c);
}

constexpr
unsigned countarg()
{
	return 0;
}

#define ARGC( ... ) countarg( #__VA_ARGS__ )








int main()
{
	std::cout
		<< ARGC() << std::endl
		<< ARGC(1) << std::endl
		<< ARGC(one, two) << std::endl
		<< ARGC("abc", 123, XYZ) << std::endl
		<< ARGC(unknown = 0, red = 1, green = 2, blue = 4) << std::endl
		<< ARGC("1", "2", "3", "4", "5") << std::endl
		<< "Wrong (comma must be escaped):" << ARGC("This is a comma: ,") << std::endl
		<< "Fine: " << ARGC("This is a comma: \x2c");


	std::cout << std::endl << std::endl;

	constexpr int eee = 40;




	return 0;
}

namespace GYUU
{
#define JJJ() static_assert(false, "JJJx");
}

//JJJ()