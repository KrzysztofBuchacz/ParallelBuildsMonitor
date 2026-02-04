#include <iostream>
#include <vector>


int main() {
    std::vector<int> myVector;

    std::cout << "Please enter some integers: ";
    while (std::cin.peek() != '\n')
    {
        int value;
        std::cin >> value;
        myVector.push_back(value);
    }

    int sum = 0;
    for (int i : myVector) {
        sum += i;
    }
    std::cout << "sum = " << sum << std::endl;

    return sum;
}
