### Data structure
1. The child command list for a character is stored in actor._commandCache (Key: ActionType)
2. The first level command list for a character is stored in actor._baseCommand

There are also some default commands stored in other places but manipulating the above 2 dictionary can already do the job.

### How to add a command
1. In the patch function of FilterCommand (or any other places that the command list is generated), call the Debug.PrintActorCommand() to get the command list info in the log file

2. In the log file, search for the action name you want to add (i havent applied any translation mod so not sure what the action name text in the log is translated or not)

3. Look for the DeclaringType and FormatNameAndSig in the same block of the log. This is the class and method that the action command will invoke

4. Implement a action delegate in the code, to invoke the method from this delegate (you can refer to DelegateActionMMFInit in the ccode)
   (Note: there are also some action delegates property in the code by Illusion but they are not covering all action command. You can still reuse them if you find one)

5. In your patching function, create a new ActionCommand variable
    - The ActionInfo setup following the info found from the log in step (2) and put it in the parameter info
    - The action delegate created in step (4) and put it in the parameter onExecute
    - For the parameter condition you can leave it. You can also supply one if you know the method used by Illusion or you have implemented your own

6. Add the new ActionCommand to the _baseCommand or _commandCache, depends on where you want the command located.

7. In most of the cases, the DeclaringType and FormatNameAndSig will point to the method ActionPoint.__c__DisplayClass150_1._Init_b__1() first. 
   You can see that in the action delegate DelegateActionMMFInit, a local variable of another obfuscated class name is created with the parameter action and action point set
   You will have to set it so that the correct function is triggered.
   
8. Create a function to patch the method you found in step (3). In the function log down the action related info and the action point from the __instance. 
   You should get the declaring type and the method name that you need to call next
   
9. Implement another action delegate to call the method you found in step (8). (you can refer to DelegateActionMMF)   
   
10. Back to the action delegate created in step (4), set the parameter for the local variable.

11. If your command is to generate a list of actors(like summon list or talk to list), you will also have to patch the corresponding get command list method

12. If you are adding a base command followed by a list of child action commands, make sure the the key added in the _commandCache match with the action type of the base command.

13. Done!

