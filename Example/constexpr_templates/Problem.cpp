#include "stdafx.h"
#include <stdio.h>
#include <iostream>


class MyClass
{
	std::string name;
	float* valPtr;

public:
	MyClass(std::string _name, float* _valPtr = nullptr)
		: name(_name)
		, valPtr(_valPtr)
	{
	}
};

#define MYCLASS(x) MyClass _##x(#x, &x)
#define MYCLASS_NEW(name, x) \
	const char new_name[] = "zzzz"; \
	MyClass ___##new_name(new_name, &x);


//---

class OtherClass
{
public:
	float val;
	OtherClass(float _val) : val{ _val } {}
};


//------------------------------------


void Problem()
{
	float f = 1.0f;
	MYCLASS(f); //calls MyClass _f("f", &f);

	OtherClass a(2.0f);
	//MYCLASS(a.val); // I would like it to call MyClass _val("val", &a.val)
	MYCLASS_NEW(val, a.val);
}
