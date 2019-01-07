@echo PreBuildEvent.bat Begin
@rem @TIMEOUT /T 2   - doesn't work when called in VS Post Build Event
@rem @sleep 2        - unknown command
PING localhost -n 3 >NUL
@echo PreBuildEvent.bat End
