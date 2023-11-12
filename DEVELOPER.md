# 👨‍💻Developer
# Welcome to the development guide! Here we will get you familiar with everything you need to know as a developer.

## Requirements
It is recommended that you use a proper IDE like Visual Studio (not Code) or Jetbrains Rider, but be aware that some IDEs require you to share some of the profit, others offer free versions for FOSS projects, be aware that Pidgeon Render Farm also has a paid beta version, so it is not eligible for Jetbrains Rider for FOSS projects.

Actively communicate with the others in order to assure efficient development.

## Development related
- Keep variable and class **styling uniform**
    - Properties (variables of a class)
        - ``This_Is_A_Example_Property``
    - Variables within a function
        - ``this_is_a_example_variable``
    - Parameters (variables passed into a function)
        - ``this_is_a_example_parameter``
    - Functions (e.g. ``void``)
        - ``This_Is_A_Example_Function``
    - Classes
        - ``ThisIsAExampleClass``
- Pick **meaningful** variable, class and function **names**
    - Short and clear is the key
- **Comment your code!**
    - Others might want to understand it in the future too
- **Test** your changes and additions
- Don't just steal others code, **give credits**
- Do **not** attempt to insert harmful or application breaking code
- Keep your code clean and readable
    - Instead of writing if-statements like this:
        ```cs
        if (condition)
            //do something
        ```
    - write them like this:
        ```cs
        if (condition)
        {
            //do something
        }
        ```
    - although you add two more lines, you make the code a lot more readable, especially in cases with many if-statements
- Avoid third party packages
    - they might contain licenses not compatible with PRF, which is not acceptable
    - do **you** know how they actually work?

## Git(Hub) related
### Which branch to work on?
Create your own. Keep the name simple and civilized. Do not create countless branches, as they clutter the Repository.
Rule of thumb: one branch per developer.

### How to push properly?
[Beedy aka Blender Defender](https://github.com/BlenderDefender) introduced [conventional commits](https://www.conventionalcommits.org/en/v1.0.0/) here at Pidgeon Tools. So please follow the style.
- feat:     newly added features
- fix:      bug fixes
- docs:     changes on the GitHub .md files
- refactor: changes on the coding style (variable naming, code comments, code optimizations)

### How do I get my changes applied to the Main (formerly Master) branch?
After you are done with all your changes create a ``pull-request`` somebody familiar with PRF from Pidgeon Tools will review your changes and merge them into the Main branch.
**Be sure to properly explain your changes, comment and document your code!**

## How do I get credited?
GitHub automatically does this as soon as you push