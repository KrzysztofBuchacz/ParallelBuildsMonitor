#include "stdafx.h"
#include <stdio.h>
#include <iostream>


template<unsigned int N>
struct Array { int a[N]; };

template<typename T>
constexpr T maxx(T f, T r) {
	return (f > r) ? f : r;
}

template<int N>
void fff(Array<N>);




extern void Problem();
extern void Left();


int main()
{
	//std::cout << "TEST" << std::endl;
	//int z = maxx<int>(7, 9);
	//std::cout << z << std::endl;

	//fff(Array<6>());

	//Problem();
	Left();

    return 0;
}

